Output tapes/03-safety-nets.gif

Require buf
Require dotnet
Require git

Set Shell "zsh"
Set FontSize 20
Set Width 1400
Set Height 700
Set Theme "Catppuccin Frappe"
Set WindowBar Colorful
Set TypingSpeed 40ms
Set Padding 20

# Setup
Hide
Type "cd /Users/jfowler/Code/buf-demo"
Enter
Sleep 500ms
Type "clear"
Enter
Sleep 500ms
Show

Type "git checkout demo/break-proto 2>&1 | tail -1"
Enter
Sleep 1s

Type "git diff demo/step-1-proto..demo/break-proto -- '*.proto'"
Enter
Sleep 3s

Type "buf breaking --against '.git#branch=demo/step-1-proto'"
Enter
Sleep 3s

Sleep 2s

# Mapper drift
Type "git checkout demo/break-mapper 2>&1 | tail -1"
Enter
Sleep 1s

Type "dotnet build API 2>&1 | grep 'error RMG'"
Enter
Sleep 4s

Sleep 3s

# Clean up
Hide
Type "git checkout main 2>/dev/null"
Enter
Sleep 500ms
Show
