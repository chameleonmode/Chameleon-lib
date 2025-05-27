export const tones = ["sarcastic", "informative", "relatable", "straightforward"];
export const ai = {
    model: "gpt",
    decorators: {
        tone: null,
        system: "You are a helpful social media assistant.",
        prefix: "As a social media expert you know how to make perfect decisions so consider the following:",
        human: "I am a reddit content creator, who creates interesting content",
        audience: "The target audience are reddit website users",
        background: "I currently am on reddit.com and looking for content",
        suffix: "Respond as creative as possible.",
    },
};
