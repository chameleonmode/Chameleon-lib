export async function askConsole(input) {
    console.log(`Ask:${input}`);
    const rl = (await import("node:readline")).createInterface({
        input: process.stdin,
        output: process.stdout,
    });
    return new Promise((resolve) => {
        rl.question(`> `, async (answer) => {
            if (!answer.startsWith("Ans:"))
                return;
            rl.close();
            resolve(answer.slice(4).trim());
        });
    });
}
