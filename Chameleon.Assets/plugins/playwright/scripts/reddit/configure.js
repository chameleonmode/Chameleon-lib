import { Logger } from "../../lib/logger.js";
import { state } from "../../lib/index.js";
export const BASE_URL = "https://www.reddit.com";
export const args = {
    scope: "People",
    sort: "Relevance",
    filter: "All",
    artifacters: [{ type: "selections", data: ["vote"] }],
};
export const settings = {
    start: {
        urls: [],
        search: [],
        all: true,
        new: true,
        attempts: 9,
        feature: "reddit",
        rando: { min: 0, max: 0 },
        iterations: { min: 0, max: 0 },
        variations: { min: 0, max: 0 },
    },
    timeouts: {
        navigate: 60,
        default: 30,
        wait: 15,
        artifacto: { delay: 120 },
        naps: { min: 256, max: 512 },
    },
};
export const ai = {
    model: "o4-mini",
    decorators: {
        system: `You are a Reddit-native assistant trained to generate relevant, tone-matching, socially appropriate information for Reddit`,
        human: "Reddit-native content creator",
        audience: "Reddit-native website users relevant to the current context in the task data",
        background: "Browsing reddit for relevant content and interacting with the Reddit community",
        tone: "adaptive to the relevant task data and context",
    },
};
export async function configure(ctx, opts) {
    Logger.debug("Opts", { opts });
    const search = opts?.settings?.start?.search || [];
    const urls = [
        ...(opts?.settings?.start?.urls || []),
        ...(search.length && !opts?.settings?.start?.urls?.length ? [BASE_URL] : [])
    ].filter(Boolean);
    state.testing = false;
    if (state.testing) {
        Logger.debug("Testing mode enabled, using provided URLs and search terms.");
        args.scope = "Posts";
        args.sort = "Relevance";
        args.filter = "All";
        search.push("joe rogan");
        urls.push(BASE_URL);
        settings.start.attempts = 1;
        settings.start.new = false;
        settings.start.rando = { min: 19, max: 3 };
        settings.start.iterations = { min: 1, max: 1 };
        settings.start.variations = { min: 1, max: 1 };
        ai.model = "grok-4";
        Logger.warn("No search terms or URLs provided, using default values.");
    }
    const options = {
        run: opts?.run ?? {},
        args: { ...args, ...opts?.args },
        settings: {
            start: {
                ...settings.start,
                ...opts?.settings?.start,
                urls,
                search,
            },
            timeouts: {
                ...settings.timeouts,
                ...opts?.settings?.timeouts,
                navigate: 1000 * 60,
                default: 1000 * 30,
                wait: 1000 * 15,
            },
        },
        ai: {
            model: opts?.ai?.model || state.ai?.model || "o4-mini",
            decorators: {
                ...ai.decorators,
                ...opts?.ai?.decorators,
            },
        },
    };
    options.settings.start.rando.max = options.settings.start.rando.min;
    options.settings.start.iterations.max = options.settings.start.iterations.min;
    options.settings.start.variations.max = options.settings.start.variations.min;
    options.settings.timeouts.naps.multiplier = undefined;
    options.settings.timeouts.naps.max = options.settings.start.variations.min + 512;
    options.settings.timeouts.artifacto.delay = 1000 * options.settings.timeouts.artifacto.delay;
    Logger.debug("Options", options);
    const page = options.settings.start.new ? await ctx.newPage() : ctx.pages()[ctx.pages().length - 1];
    return { page, options };
}
