import { Logger } from "../lib/logger.js";
import { delay } from "../lib/utils.js";
export class Player {
    actor;
    state = {
        visited: [],
        iterations: [],
    };
    constructor(actor) {
        this.actor = actor;
    }
    async play() {
        Logger.log("Delay", { delay: this.actor.opts.settings.timeouts.artifacto.delay });
        const length = this.actor.opts.settings.start.urls.length;
        for (let j = 0; j < length; j++) {
            const url = this.actor.opts.settings.start.urls[j];
            if (!url)
                continue;
            if (j > 0)
                await delay(this.actor.opts.settings.timeouts.artifacto.delay);
            Logger.log(`Url: ${j + 1} of ${length}`, url);
            while (!((await this.actor.onTry(url)) instanceof Error)) {
                this.state.visited.length = 0;
                for (let i = 0; i < this.actor.opts.settings.start.iterations.max; i++) {
                    Logger.log(`Iteration: ${i + 1} of ${this.actor.opts.settings.start.iterations.max}`);
                    if (i > 0) {
                        await delay(this.actor.opts.settings.timeouts.artifacto.delay);
                        await this.actor.onIteration(url);
                    }
                    const resulto = await this.actor.scenario(url);
                    if (resulto && typeof resulto === "number")
                        this.state.visited.push(resulto);
                }
            }
            this.state.iterations.push(j);
        }
    }
}
