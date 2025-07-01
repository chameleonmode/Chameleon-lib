import { spawn } from "child_process";
import { Logger } from "./logger.js";
export const delay = (ms) => {
    return new Promise((resolve) => {
        setTimeout(() => resolve(ms), ms);
    });
};
export async function sleepo({ min = 256, max = 512, multiplier = 0 } = {}) {
    const ms = random(min, max);
    const span = Math.floor(ms * (multiplier > 0 ? multiplier : rando(3, 6)));
    return await delay(span);
}
export function random(...values) {
    const smallest = Math.min(...values);
    const largest = Math.max(...values);
    const floor = Math.floor(Math.random() * (largest - smallest) + smallest);
    return floor;
}
export function rando(thing, thinger) {
    return Array.isArray(thing)
        ? thing[Math.floor(Math.random() * thing.length)]
        : thing && thinger
            ? random(thinger, thing)
            : thing && typeof thing === "number"
                ? Math.floor(Math.random() * thing)
                : Math.random() < 0.5;
}
export async function tryForEach(promises) {
    const fulfilled = [];
    const errors = [];
    await Promise.allSettled(promises).then((outcomes) => outcomes.forEach((outcome, index) => {
        if (outcome.status === "fulfilled") {
            fulfilled.push(outcome.value);
        }
        else {
            errors.push(outcome.reason);
        }
    }));
    return { fulfilled, errors };
}
export async function trySequentially(promises, { first = true } = {}) {
    const fulfilled = [];
    const errors = [];
    for (let i = 0; i < promises.length; i++) {
        try {
            const filled = await promises[i]();
            fulfilled.push(filled);
            if (first)
                break;
        }
        catch (error) {
            errors.push(error);
        }
    }
    return { fulfilled, errors };
}
export async function tryOnFirst(promises) {
    const errors = [];
    const racingPromises = promises.map((promise, index) => promise.catch((error) => {
        errors[index] = error;
        return new Promise(() => { });
    }));
    const fallbackPromise = Promise.all(promises.map((p, index) => p.catch((err) => {
        if (!errors[index])
            errors[index] = err;
        return null;
    }))).then(() => {
        throw new Error("All promises rejected");
    });
    try {
        const result = await Promise.race([...racingPromises, fallbackPromise]);
        return { result, errors };
    }
    catch (error) {
        return { errors };
    }
}
export function deepMerge(target, source) {
    if (!source)
        return target;
    const output = { ...target };
    Object.keys(source).forEach((key) => {
        if (source[key] instanceof Object && key in target) {
            output[key] = deepMerge(target[key], source[key]);
        }
        else {
            output[key] = source[key];
        }
    });
    return output;
}
export function getOSName() {
    const osType = process.platform;
    if (osType === "darwin")
        return "macOS";
    if (osType === "win32")
        return "Windows";
    return "Linux";
}
export function getChromePath() {
    switch (process.platform) {
        case "win32":
            return process.arch === "x64"
                ? "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
                : "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
        case "darwin":
            return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
        case "linux":
            return "/usr/bin/google-chrome";
        default:
            return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
    }
}
export async function launcher() {
    const child = spawn(getChromePath(), [`--remote-debugging-port=9613`, `--user-data-dir=/Users/dev/src/chameleon-playwright/.cache/examples`], {
        detached: true,
        stdio: "ignore",
    });
    child.unref();
    await delay(3000);
}
export function er(message, cause) {
    const error = new Error(`${message}`, { cause });
    const pretty = { cause, stack: error.stack };
    Logger.error(`(error): ${error.message}`, pretty);
    return error;
}
export function bang(message, expect, source, { print = true, caller = Logger.getCallerLine() } = {}) {
    if (print) {
        Logger.debug(`bang`, `\x1b[38;5;208mmessage:\x1b[0m`, message, `\n`, `expect:`, expect, `\n`, `source:`, source, `\n`, "caller: {\n\t", caller.method, `\n\t`, caller.filename, "\n", "}");
    }
    if (expect)
        return expect;
    throw er(message, { source, expect });
}
export function bing(message, expect, returnz, source) {
    const caller = Logger.getCallerLine();
    if (bang(message, expect, source, { caller }))
        return returnz;
    throw er(message, { source, expect });
}
