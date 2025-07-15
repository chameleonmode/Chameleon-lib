import { Logger } from "../lib/logger.js";
import { delay } from "../lib/utils.js";
export class Player {
    actor;
    iterations = [];
    constructor(actor) {
        this.actor = actor;
    }
    async play() {
        Logger.info("Delay", { delay: this.actor.opts.settings.timeouts.artifacto.delay });
        const length = this.actor.opts.settings.start.urls.length;
        for (let j = 0; j < length; j++) {
            const url = this.actor.opts.settings.start.urls[j];
            if (!url)
                continue;
            if (j > 0)
                await delay(this.actor.opts.settings.timeouts.artifacto.delay);
            Logger.info(`Url #${j + 1} of ${length}`, url);
            while (!((await this.actor.onWhile(url)) instanceof Error)) {
                for (let i = 0; i < this.actor.opts.settings.start.iterations.max; i++) {
                    Logger.info(`Iteration #${i + 1} of ${this.actor.opts.settings.start.iterations.max}`);
                    if (i > 0)
                        await this.actor.onReIteration(url);
                    const resulto = await this.actor.scenario(url);
                    Logger.info("Scenario Result", resulto);
                }
            }
            this.iterations.push(j);
        }
    }
}
