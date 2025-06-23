import { Logger } from "../../lib/logger.js";
export const BASE_URL = "https://www.reddit.com";
export const args = {
    search: ["popeye"],
    scope: "Posts",
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
        human: "reddit content creator",
        audience: "reddit website users",
        background: "surfing reddit",
        tone: "adaptive to the general tone of provided context",
    },
};
export function configure(opts) {
    Logger.debug("Opts", { opts });
    const search = opts?.args?.search || [];
    const urls = [
        ...(opts?.settings?.start?.urls || []),
        ...settings.start.urls,
    ];
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
