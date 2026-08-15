import os
import re

scenes_dir = r"Assets\Scenes"

for root, _, files in os.walk(scenes_dir):
    for file in files:
        if file.endswith(".unity"):
            path = os.path.join(root, file)
            print(f"Processing {path}...")
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            
            new_content = re.sub(r'm_MatchWidthOrHeight: 0\b', 'm_MatchWidthOrHeight: 0.5', content)
            
            if new_content != content:
                with open(path, "w", encoding="utf-8") as f:
                    f.write(new_content)
                print(f"Fixed {path}")
