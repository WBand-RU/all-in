using System.Net.Sockets;
using Microsoft.Playwright;

const string baseUrl = "https://bandfix.by";
const string storeDirectory = "~/repos/wband-all-in/Utils/BandfixGrabber/store";

IPlaywright playwright = await Playwright.CreateAsync();

IBrowserContext browserContext = await playwright.Chromium.LaunchPersistentContextAsync(
    storeDirectory,
    new BrowserTypeLaunchPersistentContextOptions() { BaseURL = baseUrl, Headless = true }
);

IPage page = await browserContext.NewPageAsync();

await page.GotoAsync("/app/songs");

var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { });

Console.WriteLine($"Found: {await link.GetAttributeAsync("name")}");
