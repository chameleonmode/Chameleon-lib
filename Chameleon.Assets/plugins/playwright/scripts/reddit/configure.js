import { Logger } from "../../lib/logger.js";
export const BASE_URL = "https://www.reddit.com";
export const args = {
    search: [],
    scope: "People",
    sort: "Relevance",
    filter: "All",
    artifacters: [{ type: "selections", data: ["vote"] }],
};
export const settings = {
    start: {
        urls: [],
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
        system: "You are a Reddit-native assistant",
        human: "reddit content creator",
        audience: "reddit website users",
        background: "surfing reddit",
        tone: "adaptive",
    },
};
export function configure(opts) {
    const search = opts?.args?.search || args.search;
    const urls = [
        ...(opts?.settings?.start?.urls || []),
        ...settings.start.urls,
    ];
    if (!search.length && !urls.length) {
        args.scope = "Posts";
        args.sort = "Relevance";
        args.filter = "All";
        urls.push("https://www.reddit.com/r/cartoons/comments/1066oh1/anyone_remember_this_this_show_was_such_an/");
        settings.start.attempts = 12;
        settings.start.new = false;
        settings.start.rando = { min: 9, max: 9 };
        settings.start.iterations = { min: 1, max: 1 };
        settings.start.variations = { min: 1, max: 1 };
        Logger.warn("No search terms or URLs provided, using default values.");
    }
    Logger.debug("Opts", { opts });
    const options = {
        run: opts?.run ?? {},
        args: { ...args, ...opts?.args },
        settings: {
            start: {
                ...settings.start,
                ...opts?.settings?.start,
                urls: [
                    ...(search.length && !urls.length ? [BASE_URL] : []),
                    ...urls,
                ].filter(Boolean),
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
            model: ai.model,
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
    return options;
}
