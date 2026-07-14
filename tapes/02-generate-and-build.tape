Output tapes/02-generate-and-build.gif

Require buf
Require dotnet
Require atlas

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
Type "cd /Users/jfowler/Code/buf-demo && git checkout demo/step-3-backend 2>/dev/null"
Enter
Sleep 1s
Type "export PATH=$PATH:$HOME/.dotnet/tools"
Enter
Sleep 500ms
# Remove the pre-committed migration so we can generate it live
Type "rm -f migrations/*add_tracks* && atlas migrate hash --env local 2>/dev/null"
Enter
Sleep 1s
Type "clear"
Enter
Sleep 500ms
Show

# Generate
Type "buf generate"
Enter
Sleep 3s

Type "git diff demo/step-1-proto..demo/step-2-generate --stat"
Enter
Sleep 3s

# Build
Type "dotnet build API 2>&1 | tail -3"
Enter
Sleep 4s

# Generate migration
Type "atlas migrate diff add_tracks --env local"
Enter
Sleep 4s

# Show migration
Type "cat migrations/*add_tracks*"
Enter
Sleep 4s
