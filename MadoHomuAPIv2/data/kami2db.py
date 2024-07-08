import json
import sqlite3

with open('kami.json', encoding='utf-8') as f:
    kami = json.load(f)

db = sqlite3.connect('main.db')
cur = db.cursor()

for id in kami:
    cur.execute('INSERT INTO kami VALUES (?, ?, ?, ?, ?, ?, ?)', (id, kami[id]['timestamp'], kami[id]['name'], kami[id]['uid'], kami[id]['article'], None, None))

db.commit()
