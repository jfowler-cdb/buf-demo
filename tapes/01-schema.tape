Output tapes/01-schema.gif

Require buf
Require git

Set Shell "zsh"
Set FontSize 18
Set Width 1400
Set Height 750
Set Theme "Catppuccin Frappe"
Set WindowBar Colorful
Set TypingSpeed 30ms
Set Padding 20

# Setup: make sure we're on the right branch
Hide
Type "cd /Users/jfowler/Code/buf-demo && git checkout demo/step-1-proto 2>/dev/null"
Enter
Sleep 1s
Type "clear"
Enter
Sleep 500ms
Show

# Show the Track message with validations (lines 17-40)
Type "sed -n '17,40p' proto/cdbaby/demo/v1beta1/tracks.proto"
Enter
Sleep 4s

# Lint
Type `buf lint && echo "✓ All lint rules pass"`
Enter
Sleep 2s

# Breaking changes
Type `buf breaking --against '.git#branch=main' && echo "✓ No breaking changes"`
Enter
Sleep 3s
