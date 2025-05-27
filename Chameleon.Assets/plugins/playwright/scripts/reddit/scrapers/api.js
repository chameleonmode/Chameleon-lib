import { Logger } from "../../../lib/logger";
import { post, articles } from "./articles";
import * as fs from "fs";
export const SELECTORS = {
    post: {
        container: 'shreddit-post, .Post, [data-testid="post-container"]',
        title: 'h1[slot="title"], h1[id^="post-title-"], .post-title',
        creditBar: '[slot="credit-bar"], .post-meta-info, [data-testid="post-metadata"]',
        subredditLink: 'a[href^="/r/"], .subreddit-link',
        subredditName: 'a.subreddit-name, [data-testid="subreddit-name"], a[href^="/r/"]',
        mediaContainer: '[slot="post-media-container"], .media-container, [data-testid="post-media"]',
        externalLink: 'a[href^="http"]:not([href*="reddit.com"]), .external-link, [data-testid="external-link"]',
        commentSection: 'faceplate-partial[name^="TopComments_"], .comments-container, [data-testid="comments-section"]',
    },
    comment: {
        container: '.Comment, shreddit-comment, [data-testid="comment"]',
        author: 'a.author-name, [slot="authorName"] a, [data-testid="comment_author"]',
        flair: '.AuthorFlair, [slot="authorFlair"], .comment-author-flair',
        score: '.score, [slot="score"], [data-test-id="comment-upvotes"], .icon-upvote + span',
        timestamp: 'faceplate-timeago, time, [data-testid="comment_timestamp"]',
        content: '.md, .RichTextJSON-root, [data-testid="comment-content"], .comment-content',
        actions: '.comment-actions, [slot="actions"], .action-buttons',
        replies: '.replies, .children, [slot="replies"]',
        awards: '.comment-awards, [slot="awards"]',
        distinguished: '.distinguished, [data-testid="distinguished-text"]',
        collapsed: '.collapsed, [data-testid="collapsed-comment"]',
    },
    subreddit: {
        feed: "shreddit-feed article",
        postTitle: 'a[slot="title"], a[id^="post-title-"], [slot="title"]',
        authorName: '[slot="authorName"] a, .advertiser-name',
        flair: '[slot="post-flair"] .flair-content',
        mediaImage: "img.preview-image, img.preview-img, img.media-lightbox-img",
        thumbnail: '[slot="thumbnail"] img, .thumbnail img',
        commentLink: [
            'a[data-testid="comment-link"]',
            'a[data-click-id="comments"]',
            'span:has-text("comments")',
            'a:has-text("comments")',
            '[slot="comments-button"]',
        ].join(", "),
        titleLink: 'h1 a, h3 a, a[data-click-id="body"]',
    },
};
async function navigateToPost(page, permalink) {
    const postUrl = `https://www.reddit.com${permalink}`;
    const postPermalink = new URL(postUrl).pathname;
    Logger.info(`Attempting to navigate to post: ${postPermalink}`);
    const safele = async (selector) => {
        const elements = await page.$$(selector);
        for (const element of elements) {
            const isInsideMedia = await element.evaluate((el) => {
                const mediaIdentifiers = [
                    (el) => el.tagName.toLowerCase().includes("player"),
                    (el) => el.tagName.toLowerCase() === "video",
                    (el) => el.tagName.toLowerCase() === "iframe",
                    (el) => el.getAttribute("slot") === "post-media-container",
                    (el) => el.classList.contains("media-container"),
                    (el) => el.tagName.toLowerCase().includes("shreddit-player"),
                    (el) => el.hasAttribute("autoplay"),
                    (el) => el.hasAttribute("post-type"),
                    (el) => el.hasAttribute("preview"),
                    (el) => el.hasAttribute("data-post-click-location"),
                    (el) => el.hasAttribute("caption-url"),
                    (el) => el.classList.contains("pointer-cursor"),
                    (el) => {
                        const parent = el.parentElement;
                        return (parent &&
                            (parent.classList.contains("relative") ||
                                parent.classList.contains("overflow-hidden") ||
                                parent.classList.contains("pointer-cursor") ||
                                parent.classList.contains("isolate") ||
                                parent.getAttribute("slot") === "post-media-container"));
                    },
                ];
                let current = el;
                while (current) {
                    if (mediaIdentifiers.some((check) => check(current))) {
                        return true;
                    }
                    const parent = current.parentElement;
                    if (!parent)
                        break;
                    current = parent;
                }
                return false;
            });
            if (!isInsideMedia) {
                Logger.info(`Found safe clickable element: ${selector}`);
                await element.click({ force: true });
                return true;
            }
        }
        Logger.info(`No safe clickable elements found for: ${selector}`);
        return false;
    };
    const strategies = [
        {
            name: "direct",
            fn: async () => {
                Logger.info("Using direct navigation");
                await page.goto(postUrl, { waitUntil: "load", timeout: 30000 });
                return true;
            },
        },
        { name: "title", fn: () => safele(SELECTORS.subreddit.titleLink) },
        {
            name: "permalink",
            fn: () => safele(`a[href="${postPermalink}"], a[href*="${postPermalink}"]`),
        },
        { name: "comments", fn: () => safele(SELECTORS.subreddit.commentLink) },
        {
            name: "js-redirect",
            fn: async () => {
                Logger.info("Using JavaScript navigation");
                await page.evaluate((url) => {
                    window.location.href = url;
                }, postUrl);
                await page.waitForLoadState("load");
                return true;
            },
        },
    ];
    return strategies;
}
export async function scrapeSubreddit(page, maxPosts = 5) {
    for (let i = 0; i < 3; i++) {
        await page.evaluate(() => {
            window.scrollBy(0, window.innerHeight);
        });
        await page.waitForTimeout(1000);
    }
    await page.waitForSelector(SELECTORS.subreddit.feed, { timeout: 30000 });
    await page.waitForTimeout(1000);
    const feed = await articles(page);
    for (let i = 0; i < Math.min(feed.length, maxPosts); i++) {
        const article = feed[i];
        const currentUrl = page.url();
        const back = async () => {
            while (page.url() !== currentUrl) {
                await page.goBack({ waitUntil: "domcontentloaded" });
                await page.waitForTimeout(1000);
            }
        };
        try {
            for (const { name, fn } of await navigateToPost(page, article.permalink)) {
                Logger.info(`Trying navigation strategy: ${name}`);
                if (await fn()) {
                    await page.waitForLoadState("load");
                    await page.waitForTimeout(1000);
                    const currentUrl = page.url();
                    if (currentUrl.includes(article.permalink)) {
                        Logger.info(`Successfully navigated to post via ${name} strategy`);
                        article.post = await post(page);
                        break;
                    }
                    else {
                        Logger.warn(`Failed to navigate to post via ${name} strategy`);
                        await back();
                    }
                }
            }
            Logger.info(`Scraped post data for ${article.title}`, article);
        }
        catch (error) {
            Logger.warn(`Error navigating to post: ${error}`, error);
            feed.splice(i, 1);
            i--;
        }
        finally {
            Logger.info("Returning to subreddit page");
            await back();
        }
    }
    const outputPath = `.cache/scraper/./feed.json`;
    fs.writeFileSync(outputPath, JSON.stringify(feed, null, 2));
    return feed;
}
