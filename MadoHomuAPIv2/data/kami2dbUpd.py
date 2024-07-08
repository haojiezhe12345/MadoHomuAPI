import json
import sqlite3

with open('kami.json', encoding='utf-8') as f:
    msgs = json.load(f)

db = sqlite3.connect('main.db')
cur = db.cursor()

cur.execute('SELECT max(id) FROM kami')
maxid = cur.fetchone()[0]

for id in msgs:
    if int(id) > maxid:
        cur.execute('INSERT INTO kami VALUES (?, ?, ?, ?, ?, ?, ?)', (id, msgs[id]['timestamp'], msgs[id]['name'], msgs[id]['uid'], msgs[id]['article'], None, None))

db.commit()
