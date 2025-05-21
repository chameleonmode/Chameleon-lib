import { Logger } from "../lib/logger.js";
export class Player {
    actor;
    visited = [];
    constructor(actor) {
        this.actor = actor;
    }
    async play() {
        const length = this.actor.opts.settings.start.urls.length;
        for (let j = 0; j < length; j++) {
            const url = this.actor.opts.settings.start.urls[j];
            if (!url)
                continue;
            Logger.log(`Url: ${j + 1} of ${length}`, url);
            while (!((await this.actor.onTry(url)) instanceof Error)) {
                this.visited.length = 0;
                for (let i = 0; i < this.actor.iterations; i++) {
                    Logger.log(`Iteration: ${i + 1} of ${this.actor.iterations}`);
                    if (i > 0)
                        await this.actor.onIteration(url);
                    const resulto = await this.actor.scenario(url);
                    if (resulto && typeof resulto === "number")
                        this.visited.push(resulto);
                }
            }
        }
    }
}
