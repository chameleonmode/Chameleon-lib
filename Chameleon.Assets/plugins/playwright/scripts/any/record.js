export default async function (context) {
    const page = await context.newPage();
    await page.pause();
}
