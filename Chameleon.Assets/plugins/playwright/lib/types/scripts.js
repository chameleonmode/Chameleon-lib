export const settings = {
    start: {
        all: true,
        new: true,
        attempts: 9,
        feature: "reddit",
        rando: { min: 1, max: 1 },
        iterations: { min: 1, max: 1 },
        variations: { min: 1, max: 1 },
        urls: [],
    },
    timeouts: {
        navigate: 60,
        default: 30,
        wait: 15,
        naps: { min: 256, max: 512 },
    },
};
