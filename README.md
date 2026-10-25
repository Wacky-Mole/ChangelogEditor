# Description

## A client-side changelog window for the main menu.

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly, upon file save, it will auto update your configurations.`

### `This is a client only mod!`

<div style="text-align: center;">
  <a href="https://www.survivalservers.com/services/game_servers/valheim/?ref=WackyMole">
    <img
      src="https://www.wackymole.com/hosts/WackySurvivalBanner.png"
      alt="Valheim Server Hosting - Survival Servers"
      style="display: inline-block; width: 768px; max-width: 100%; height: auto;">
  </a>
</div>

---

The configuration file is located at `BepInEx/config/WackyMole.ChangelogEditor.cfg`

The content file is read from `BepInEx/config/WackyMole.ChangelogEditor.txt`

### Independent window

Use the original changelog button in the main menu. It opens a separate, scrollable IMGUI window in the bottom-left, above the button area, with a Close button above the scrolling text; Escape also closes it. The mod redirects the original opening handler without replacing its button listeners. The window does not use the game's changelog panel, so mods controlling that panel do not control this window.

Under `[1 - Changelog]`:

- `Should Show Changelog` controls visibility of the original changelog button.
- `Force Show Changelog` opens the independent window whenever the main-menu scene starts, even if the original button is hidden.
- `Should Change Text` adds custom text before the game's changelog; `Override Changelog Text` shows only custom text and takes precedence.
- `Title Text` and `Width` control the window title and width. Width is limited to the screen size.

Text/config edits update the independent window without restarting. The window is destroyed with the menu scene and recreated when returning from gameplay. This does not establish or fix the cause of unrelated logout crashes.

The window supports Unity IMGUI rich text, including color, bold, italic, and size tags. TextMeshPro-specific tags such as sprites are not supported.

Write the content file as UTF-8 with normal line breaks and accented characters. Literal `\r\n`, `\n`, and `\r` sequences also become line breaks; `\t` becomes a tab, and `\uXXXX` escapes become Unicode characters (for example, `\u00f3` becomes ó). Color tags are preserved. No surrounding quotes are needed.

The ChangeLog supports using colors, so go wild? Examples below.

```txt
<color=red>YOUR RED TEXT HERE</color>
<color=blue>YOUR BLUE TEXT HERE</color>
<color=green>YOUR GREEN TEXT HERE</color>
<color=yellow>YOUR YELLOW TEXT HERE</color>
<color=orange>YOUR ORANGE TEXT HERE</color>
<color=#00afd4>YOUR CUSTOM COLOR TEXT HERE</color>
```



<br>
<br>

`Feel free to reach out to me on discord if you need manual download assistance.`

#  Author Information

## WackyMole

For questions or suggestions please join discord channel: [Odin Plus Team](https://discord.gg/jkcJCq2sK5) or my discord at [Wolf Den](https://discord.gg/uPjjH8y52j)


### Previous Author: Azumatt

`DISCORD:` Azumatt#2625

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)]