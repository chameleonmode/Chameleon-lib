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
    model: "gpt",
    decorators: {
        tone: "adaptive to the general tone of context",
        system: "You are helpful!",
        prefix: "As a social media expert you know how to make perfect decisions so consider the following:",
        human: "reddit content creator",
        audience: "adaptive to the general audience of the task context",
        background: "surfing reddit",
        suffix: "Respond as creative as possible.",
    },
};
export function configure(opts) {
    Logger.debug("Opts", { opts });
    const search = opts?.args?.search || args.search;
    const options = {
        args: {
            ...args,
            ...opts?.args,
        },
        run: { ...opts?.run },
        settings: {
            ...settings,
            start: {
                ...settings.start,
                ...opts?.settings?.start,
                urls: [
                    ...(search.length ? [BASE_URL] : []),
                    ...(settings.start.urls.length ? settings.start.urls : []),
                ].filter(Boolean),
            },
            timeouts: {
                ...settings.timeouts,
                ...opts?.settings?.timeouts,
                navigate: 1000 * settings.timeouts.navigate,
                default: 1000 * settings.timeouts.default,
                wait: 1000 * settings.timeouts.wait,
            },
        },
        ai: {
            model: ai.model,
            decorators: {
                tone: ai.decorators.tone,
                system: opts?.ai?.decorators.system || ai.decorators.system,
                prefix: opts?.ai?.decorators.prefix || ai.decorators.prefix,
                human: opts?.ai?.decorators.human || ai.decorators.human,
                audience: opts?.ai?.decorators.audience || ai.decorators.audience,
                background: opts?.ai?.decorators.background || ai.decorators.background,
                suffix: opts?.ai?.decorators.suffix || ai.decorators.suffix,
            },
        },
    };
    options.settings.start.rando.max = options.settings.start.rando.min;
    options.settings.start.iterations.max = options.settings.start.iterations.min;
    options.settings.start.variations.max = options.settings.start.variations.min;
    options.settings.timeouts.artifacto.delay = 1000 * options.settings.timeouts.artifacto.delay;
    Logger.debug("Options", options);
    return options;
}
