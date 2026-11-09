# Gergroen developer blog

Developer blog with getting started guides, built with **.NET 10 Blazor WebAssembly**, hosted on
**GitHub Pages** and installable as a **Progressive Web App**.

Live: https://gergroen.github.io/blog/

## Writing a post

1. Add a Markdown file to [`src/Blog/wwwroot/posts`](src/Blog/wwwroot/posts), named `yyyy-MM-dd-your-slug.md`.
   The part after the date becomes the URL: `/blog/post/your-slug`.
2. Start it with front matter:

   ```markdown
   ---
   title: "My post title"
   date: 2026-11-09            # optional, defaults to the date in the file name
   tags: [.NET, C#]
   summary: One or two sentences for the post list, RSS feed and search engines.
   draft: true                 # optional: visible locally, never published
   ---
   ```

3. Write the post in Markdown. Use fenced code blocks with a language (`csharp`, `xml`, `json`, `powershell`,
   `bash`, `yaml`, ...) for syntax highlighting.
4. Put images in `src/Blog/wwwroot/posts/images/` and reference them as `![alt](images/picture.png)`.
5. Preview locally:

   ```powershell
   dotnet watch --project src/Blog
   ```

   The post index is generated during the build. If you change a post's front matter while `dotnet watch`
   is running, restart it (Ctrl+R) to pick up the change.
6. Commit and push to `main`. GitHub Actions tests, builds and deploys the blog in a few minutes.
   Visitors with the installed app get a "new version available" banner.

## How it works

| Part | Where |
| --- | --- |
| Blazor app (pages, layout, post loading) | [src/Blog](src/Blog) |
| Shared model, slugs, Markdown rendering (Markdig) | [src/Blog.Core](src/Blog.Core) |
| Build tool: `index.json`, `sitemap.xml`, `feed.xml`, and GitHub Pages post-processing | [tools/Blog.Indexer](tools/Blog.Indexer) |
| Tests | [tests/Blog.Tests](tests/Blog.Tests) |
| Deployment | [.github/workflows/deploy.yml](.github/workflows/deploy.yml) |

- **Build:** an MSBuild target in `Blog.csproj` runs the indexer, which reads the front matter of all posts and
  generates `posts/index.json`, `sitemap.xml` and `feed.xml`. These files are git-ignored. Drafts are only
  included in Debug builds.
- **Runtime:** the app loads `posts/index.json`. When you open a post it fetches the `.md` file, renders it with
  Markdig, and highlights code with highlight.js (Stack Overflow theme).
- **GitHub Pages:** after `dotnet publish`, `Blog.Indexer postpublish`:
  - sets `<base href="/blog/">`
  - writes a static, prerendered `post/<slug>.html` per post, so deep links return 200 and search engines see content
  - updates the service worker's integrity hash of `index.html`
  - adds `.nojekyll`

  Other deep links (such as `/blog/tags/csharp`) are handled by `404.html`, which redirects to the app.
- **PWA:** `manifest.webmanifest` plus `service-worker.published.js`. The app shell is precached. Posts
  (`posts/*`) are fetched network-first and cached, so posts you've read are available offline.

## One-time GitHub setup

In the repository go to **Settings → Pages → Build and deployment** and set **Source** to **GitHub Actions**.

## Local production check

```bash
dotnet publish src/Blog -c Release -o out
dotnet run --project tools/Blog.Indexer -c Release -- postpublish --wwwroot out/wwwroot --base-path /blog/ --site-url https://gergroen.github.io/blog/
```

Then serve `out/wwwroot` under `/blog/`. In Git Bash, set `MSYS_NO_PATHCONV=1` first, so `/blog/` is not
converted to a Windows path.

## Using a custom domain later

1. Add a `CNAME` file to `src/Blog/wwwroot`.
2. Change `SITE_URL` and `BASE_PATH` (`/`) in the workflow, and `BlogSiteUrl` in `Blog.csproj`.
3. Set `pathSegmentsToKeep = 0` in `404.html`.
