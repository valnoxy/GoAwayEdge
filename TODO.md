<div align="center">

  <!-- PROJECT LOGO -->
  <br />
    <a href="https://goawayedge.com">
      <img src="https://dl.exploitox.de/goawayedge/gh-banner-goawayedge.png" alt="GoAwayEdge Banner">
    </a>
  <br />

  [![Version][version-shield]][version-url]
  [![Download Counter][downloads-shield]][downloads-url]
  [![License][license-shield]][license-url]
  [![Weblate][weblate-shield]][weblate-url]
</div>

[version-shield]: https://img.shields.io/github/v/release/valnoxy/GoAwayEdge?color=9565F6
[version-url]: https://github.com/valnoxy/GoAwayEdge/releases

[weblate-shield]: http://translate.valnoxy.dev/widget/goawayedge/svg-badge.svg
[weblate-url]: https://translate.valnoxy.dev/engage/goawayedge/

[downloads-shield]: https://img.shields.io/github/downloads/valnoxy/GoAwayEdge/total.svg?color=431D93
[downloads-url]: https://github.com/valnoxy/GoAwayEdge/releases

[license-shield]: https://img.shields.io/github/license/valnoxy/GoAwayEdge?color=9565F6
[license-url]: https://img.shields.io/github/license/valnoxy/GoAwayEdge

<div align="center">
  <h3 align="center">GoAwayEdge</h3>
  <p align="center">
    <p>Don't like Microsoft Edge? Me neither. Redirect all Edge calls to your favorite browser!</p>
    <a href="https://github.com/valnoxy/GoAwayEdge/releases">Download now</a>
    ·
    <a href="https://github.com/valnoxy/GoAwayEdge/issues">Report Bug</a>
    ·
    <a href="https://github.com/valnoxy/GoAwayEdge/discussions/">Discussions</a>
    ·
    <a href="https://translate.valnoxy.dev/engage/goawayedge/">Help me translate</a>
    <br />
    <br />
  </p>
</div>

---

# TODO list for Version 2.0

## Features
- [ ] Settings Menu
- [ ] Change functionality of Copilot key / taskbar icon
- [ ] Change Weather service
- [ ] Silent App Updater

## Quality of Life
- [ ] Redesigned parsing engine
- [ ] Redesigned logging system

# Development Notes

### Search Bar Arguments (search term: "hallo")
```
"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --single-argument microsoft-edge:?url=https%3A%2F%2Fwww.bing.com%2Fsearch%3Fq%3Dhallo%26form%3DWSBEDG%26qs%3DAS%26cvid%3D12fe0e3529de48c0815fa6882e348e59%26pq%3DHallo%26cc%3DDE%26setlang%3Dde-DE%26nclid%3DF1C9E841B291A09001E0E245264AC67E%26ts%3D1722019735480%26nclidts%3D1722019735%26tsms%3D480%26wsso%3DModerate&timestamp=1722019735480&source=WindowsSearchBox&campaign=addedgeprot&medium=AutoSuggest
```

### Copilot PWA Arguments
```
"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --app-id=khiogjgiicnghciboipemonlmgelhblf --app-fallback-url=https://copilot.microsoft.com/?dpwa=1 --display-mode=standalone --windows-store-app --ip-proc-id=41940 --ip-binding --mojo-named-platform-channel-pipe=41940.41536.15380779445885585744 --ip-aumid=Microsoft.Copilot_8wekyb3d8bbwe!App
```

### Copilot Taskbar (with Size)
```
"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --single-argument microsoft-edge:///?ux=copilot&tcp=1&source=taskbar&invocationx=1888&invocationy=1056
```

### Copilot Hotkey (WIN+C & Copilot Key)
```
"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --single-argument microsoft-edge:///?ux=copilot&tcp=1&source=hotkey
```

### PDF file
```
"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --single-argument "C:\Users\jonas\Downloads\test.pdf"
```