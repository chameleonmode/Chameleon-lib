import { Logger } from "../../../lib/logger.js";
import { SELECTORS } from "./api.js";
export async function post(page) {
    const extract = async (selector) => {
        return await page.$eval(selector, (el) => {
            const attribution = (el) => {
                const attributes = Array.from(el.attributes).reduce((acc, attr) => {
                    acc[attr.name] = attr.value;
                    return acc;
                }, {});
                return {
                    attributes,
                    tag: el.tagName,
                    text: el.textContent?.trim().replace(/\n/g, "").replace(/ +/g, " "),
                };
            };
            const elemental = (el) => {
                return {
                    attributes: attribution(el),
                    elementals: Array.from(el.children).map((child) => elemental(child)),
                };
            };
            return {
                ...elemental(el),
            };
        });
    };
    const [container] = await Promise.all([extract(SELECTORS.post.container)]);
    const post = {
        container,
        comments: await page.$$eval(SELECTORS.comment.container, (elements, selectors) => {
            return elements.map((ele) => {
                const author = ele.getAttribute("author") || ele.querySelector(selectors.author)?.textContent?.trim() || "";
                const scoreAttr = ele.getAttribute("score");
                const scoreElement = ele.querySelector(selectors.score);
                const scoreText = scoreAttr || scoreElement?.textContent?.trim() || "0";
                const score = /\d+/.test(scoreText) ? parseInt(scoreText, 10) : 0;
                const timestampAttr = ele.getAttribute("ts") || ele.querySelector("[ts]")?.getAttribute("ts");
                const timeElement = ele.querySelector(selectors.timestamp);
                const timestamp = timestampAttr ||
                    (timeElement
                        ? timeElement.getAttribute("ts") ||
                            timeElement.getAttribute("datetime") ||
                            timeElement?.textContent?.trim() ||
                            ""
                        : "");
                const contentId = ele.getAttribute("thingid");
                const contentElement = contentId
                    ? ele.querySelector(`#${contentId}-post-rtjson-content`) || ele.querySelector(selectors.content)
                    : ele.querySelector(selectors.content);
                const content = contentElement?.outerHTML.replace(/\n/g, "").replace(/ +/g, " ") || "";
                const text = contentElement?.textContent?.trim().replace(/\n/g, "").replace(/ +/g, " ") || "";
                const depthAttr = ele.getAttribute("depth");
                const depthAttributes = depthAttr ? ["depth"] : ["depth", "data-depth", "comment-depth"];
                const dataDepth = depthAttr ||
                    depthAttributes.map((attr) => ele.getAttribute(attr)).find((val) => val !== null) ||
                    "0";
                const depth = parseInt(dataDepth, 10) || 0;
                return {
                    author,
                    score,
                    timestamp,
                    depth,
                    text,
                };
            });
        }, SELECTORS.comment),
    };
    Logger.info(`Successfully scraped post:`, post, post.container, post.container?.attributes, post.container?.elementals, post.comments);
    return post;
}
export async function articles(page) {
    const articles = await page.$$eval(SELECTORS.subreddit.feed, (elements, selectors) => {
        return elements.map((post) => {
            const article = {
                postType: "unknown",
            };
            const shredditPost = post.querySelector("shreddit-post");
            if (shredditPost) {
                const getAttr = (attr) => shredditPost.getAttribute(attr) || undefined;
                article.id = getAttr("id");
                article.permalink = getAttr("permalink");
                article.url = getAttr("content-href");
                article.domain = getAttr("domain");
                article.author = getAttr("author");
                article.authorId = getAttr("author-id");
                article.created = getAttr("created-timestamp");
                article.score = getAttr("score") || "0";
                article.comments = getAttr("comment-count") || "0";
                const postType = getAttr("post-type");
                if (postType === "video")
                    article.postType = "video";
                else if (postType === "image")
                    article.postType = "image";
                else if (postType === "link")
                    article.postType = "link";
                else if (postType === "text")
                    article.postType = "text";
                else {
                    if (post.querySelector('shreddit-player-2, video, [data-test-id="video-player"]')) {
                        article.postType = "video";
                    }
                    else if (post.querySelector('img.preview-img, img.media-lightbox-img, [data-test-id="post-image"]')) {
                        article.postType = "image";
                    }
                    else if (post.querySelector('a.post-link, [data-testid="outbound-link"]')) {
                        article.postType = "link";
                    }
                    else {
                        article.postType = "text";
                    }
                }
            }
            if (!article.author) {
                const authorElement = post.querySelector(selectors.authorName);
                article.author = authorElement?.textContent?.trim().replace(/^u\//, "") || "";
                if (authorElement) {
                    const href = authorElement.getAttribute('href');
                    if (href) {
                        article.authorId = href.split('/').filter(Boolean).pop() || '';
                    }
                }
            }
            const titleElement = post.querySelector(selectors.postTitle);
            article.title = titleElement?.textContent?.trim() || "";
            if (!article.url && titleElement) {
                article.url = titleElement.getAttribute('href') || "";
            }
            if (!article.permalink) {
                const permalinkElement = post.querySelector(selectors.titleLink);
                article.permalink = permalinkElement?.getAttribute('href') || "";
            }
            if (!article.score) {
                const scoreElement = post.querySelector('[data-testid="post-score"], [data-test-id="post-score"]');
                article.score = scoreElement ? scoreElement.textContent?.trim() || "0" : "0";
            }
            if (!article.comments) {
                const commentCountElement = post.querySelector('[data-test-id="comment-count"], [data-testid="comment-count"]');
                article.comments = commentCountElement ? commentCountElement.textContent?.trim() || "0" : "0";
            }
            return article;
        });
    }, SELECTORS.subreddit);
    const validArticles = articles.filter((article) => {
        return article.title && article.permalink && article.postType !== "unknown";
    });
    Logger.info(`Successfully scraped ${validArticles.length} articles`);
    return validArticles;
}
