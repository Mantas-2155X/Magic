#!/bin/bash

cd "/media/flash/Projects/Unity/Magic/Blender/"

for f in *.blend;
do
    blender --background "$f" --python _export.py
done
