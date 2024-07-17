import os
import json
import sqlite3

users = {}


def adduser(user, timestamp):
    if user in ['', 'DELETED', '匿名用户']:
        return
    timestamp = int(timestamp)
    if user in users and timestamp >= users[user]['time']:
        return
    users[user] = {
        'time': timestamp
    }


avatarDir = R'Z:\Web\Dashboard0\madohomu\api\data\images\avatars'

for user in os.listdir(avatarDir):
    # mtime = os.path.getmtime(os.path.join(avatarDir, user))
    ctime = os.path.getctime(os.path.join(avatarDir, user))
    # if (mtime - ctime > 600):
    #     print(f'{user}  {mtime - ctime:.0f}')
    adduser(user.removesuffix('.jpg'), ctime)


db = sqlite3.connect(R"main.db")
cur = db.cursor()
for row in cur.execute("SELECT * FROM comments"):
    adduser(row[2], row[1])


users = dict(sorted(users.items(), key=lambda k: k[1]['time']))

with open('users.json', 'w', encoding='utf-8') as f:
    json.dump(users, f, ensure_ascii=False, indent=2)


ids = []

for i, user in enumerate(users):
    ids.append({
        'id': i + 1,
        'name': user,
        'time': users[user]['time'],
    })

with open('user_ids.json', 'w', encoding='utf-8') as f:
    json.dump(ids, f, ensure_ascii=False, indent=2)


cur.execute(f'DELETE FROM sqlite_sequence')
cur.execute(f'DELETE FROM users')
cur.execute('UPDATE comments SET uid = null')
for user in ids:
    cur.execute('INSERT INTO users (name, avatar, create_time) VALUES (?, ?, ?)', (
        user['name'],
        user['name'] + '.jpg' if os.path.exists(os.path.join(avatarDir, user['name'] + '.jpg')) else None,
        user['time']
    ))
    cur.execute('UPDATE comments SET uid = ? WHERE sender = ?', (user['id'], user['name']))
db.commit()

db.close()
