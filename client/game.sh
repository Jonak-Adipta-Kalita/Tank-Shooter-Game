#!/bin/sh
printf '\033c\033]0;%s\a' Tank Shooter
base_path="$(dirname "$(realpath "$0")")"
"$base_path/game.x86_64" "$@"
