import { Logger } from "../../lib/logger.js";
export const BASE_URL = "https://www.reddit.com";
export function configure(opts) {
    Logger.log("Opts", { opts });
    const args = {
        scope: "Posts",
        sort: "Relevance",
        filter: "All",
        search: ["popeye"],
        artifacters: [{ type: "selections", data: ["vote"] }],
        ...opts?.args,
    };
    const settings = {
        start: {
            new: true,
            attempts: 9,
            feature: "reddit",
            rando: { min: 1, max: 1 },
            iterations: { min: 1, max: 1 },
            variations: { min: 1, max: 3 },
            urls: [],
            ...opts?.settings?.start,
            all: true,
        },
        timeouts: {
            navigate: 60,
            default: 30,
            wait: 15,
            naps: { min: 256, max: 512 },
            ...opts?.settings?.timeouts,
        },
    };
    const ai = {
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
    const options = {
        args,
        run: { ...opts?.run },
        settings: {
            ...settings,
            start: {
                ...settings.start,
                urls: [
                    ...(args.search.length ? [BASE_URL] : []),
                    ...(settings.start.all && settings.start.urls.length ? settings.start.urls : []),
                ].filter(Boolean),
            },
            timeouts: {
                ...settings.timeouts,
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
    return options;
}
