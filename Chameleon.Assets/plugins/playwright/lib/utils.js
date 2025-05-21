export const sleep = (ms) => {
    return new Promise((resolve) => {
        setTimeout(() => resolve(ms), ms);
    });
};
export function random(...values) {
    const smallest = Math.min(...values) + 1;
    const largest = Math.max(...values) + 1;
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
export async function sleepRandom({ min = 256, max = 512, multiplier = 0 }) {
    const ms = random(min, max);
    const delay = Math.floor(ms * (multiplier > 0 ? multiplier : random(3, 6)));
    return await sleep(delay);
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
        return { fulfilled: result, errors };
    }
    catch (error) {
        return { fulfilled: null, errors };
    }
}
export async function trySequentially(promises) {
    const errors = [];
    for (let i = 0; i < promises.length; i++) {
        try {
            const fulfilled = await promises[i]();
            return { fulfilled, errors, fulfilledIndex: i };
        }
        catch (error) {
            errors.push(error);
        }
    }
    return {
        fulfilled: null,
        errors,
        fulfilledIndex: -1,
    };
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
