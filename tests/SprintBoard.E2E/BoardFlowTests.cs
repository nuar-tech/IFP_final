// tests/SprintBoard.E2E/BoardFlowTests.cs
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace SprintBoard.E2E;

[Parallelizable(ParallelScope.Self)]
public class BoardFlowTests : PageTest
{
    private const string ClientUrl = "https://localhost:7100";

    public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true };

    [Test]
    public async Task A_new_user_can_register_create_a_board_and_add_a_task()
    {
        var email = $"e2e-{Guid.NewGuid():N}@test.dev";

        await Page.GotoAsync($"{ClientUrl}/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create an account" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync("E2E User");
        await Page.GetByLabel("Email").FillAsync(email);
        await Page.GetByLabel("Password").FillAsync("Passw0rd!x");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/boards$"));

        await Page.GetByLabel("New board name").FillAsync("Demo sprint");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create board" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Demo sprint" }).ClickAsync();

        await Page.GetByLabel("New task title").FillAsync("Write the defense slides");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add task" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Write the defense slides" })).ToBeVisibleAsync();
    }
}