---
title: "Blazor WebAssembly: Getting started guide - Host a blog on GitHub Pages as an installable PWA"
date: 2026-11-09
tags: [Blazor, WebAssembly, .NET, C#, GitHub Pages, PWA, Markdown]
summary: How this blog works. A .NET 10 Blazor WebAssembly app that renders Markdown posts, deploys to GitHub Pages with GitHub Actions, and installs on your device as a Progressive Web App.
---

Welcome to my developer blog! It's all about practical getting started guides.
The first guide is about the blog itself: how to build, host and maintain a blog with
**Blazor WebAssembly** on **GitHub Pages**, and make it installable as a **Progressive Web App (PWA)**.

## What you need

- The [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A GitHub account and a repository, for example `github.com/<you>/blog`
- An editor such as Visual Studio or VS Code

## 1. Create the Blazor WebAssembly app

The `blazorwasm` template has a `--pwa` option that adds a web app manifest and a service worker:

```powershell
dotnet new blazorwasm --pwa --empty -o src/Blog
```

A Blazor WebAssembly app compiles to static files only, which is exactly what GitHub Pages can host.

## 2. Write posts in Markdown

Every post is a Markdown file in `wwwroot/posts`, with some YAML front matter at the top:

```markdown
---
title: "My first post"
date: 2026-11-09
tags: [.NET, C#]
summary: One or two sentences for the post list and search engines.
---

Hello **world**!
```

During the build, a small console tool reads the front matter of all posts and generates
`posts/index.json`, `sitemap.xml` and an RSS `feed.xml`. The app only downloads the index at startup,
and fetches a post when you open it.

## 3. Render Markdown in the browser

[Markdig](https://github.com/xoofx/markdig) runs fine in WebAssembly. A post page loads the Markdown,
converts it to HTML and renders it as a `MarkupString`:

```csharp
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()      // don't render the front matter
        .UseAdvancedExtensions()   // tables, task lists, auto identifiers, ...
        .Build();

    public static string ToHtml(string markdown) => Markdown.ToHtml(markdown, Pipeline);
}
```

```razor
@page "/post/{Slug}"
@inject PostService PostService

<article class="post-content">@((MarkupString)html)</article>
```

Code blocks are highlighted with [highlight.js](https://highlightjs.org/), using the Stack Overflow
themes.

## 4. Deploy with GitHub Actions

A workflow publishes the app on every push to `main`, and deploys the `wwwroot` folder to GitHub Pages:

```yaml
- run: dotnet publish src/Blog -c Release -o out
- uses: actions/upload-pages-artifact@v4
  with:
    path: out/wwwroot
- uses: actions/deploy-pages@v4
```

There are a few things to know when you host a Blazor app on GitHub Pages:

| Problem | Solution |
| --- | --- |
| The site runs under `/blog/`, not `/` | Set `<base href="/blog/" />` in `index.html` after publishing |
| Jekyll ignores the `_framework` folder | Add an empty `.nojekyll` file |
| Deep links such as `/blog/post/x` return a 404 | Generate a static HTML page per post, and use a `404.html` redirect for other routes |
| Changing `index.html` breaks the service worker integrity check | Recalculate its hash in `service-worker-assets.js` |

## 5. Make it an installable PWA

The `manifest.webmanifest` describes the app: its name, colors and icons. A relative `start_url` and
`scope` keep it working under `/blog/`:

```json
{
  "name": "Gergroen - Developer blog",
  "short_name": "Gergroen",
  "start_url": "./",
  "scope": "./",
  "display": "standalone"
}
```

The service worker caches the app, so it starts offline. Posts are fetched *network first*: you always get
the latest version when you're online, and the cached copy when you're not.

```javascript
async function networkFirst(request) {
    const cache = await caches.open('blog-content');
    try {
        const response = await fetch(request);
        if (response.ok) await cache.put(request, response.clone());
        return response;
    } catch (error) {
        return await cache.match(request) ?? Promise.reject(error);
    }
}
```

When a new version of the blog is deployed, the app shows a small *"A new version is available"* banner.

To install the blog, use the install icon in the address bar (Chrome or Edge on the desktop) or
*Add to Home screen* on your phone.

## Writing a new post

1. Add `src/Blog/wwwroot/posts/yyyy-MM-dd-my-post.md`
2. Preview it with `dotnet watch --project src/Blog`
3. Commit and push to `main`, and GitHub Actions publishes it in a few minutes

The complete source of this blog is on [GitHub](https://github.com/gergroen/blog).
