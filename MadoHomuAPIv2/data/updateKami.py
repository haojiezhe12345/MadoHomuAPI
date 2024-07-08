import requests
from bs4 import BeautifulSoup
from datetime import datetime
import json
import traceback
import sqlite3
from time import sleep


with open('kami.json', encoding='utf-8') as f:
    msgs = json.load(f)

while True:
    len0 = len(msgs)

    try:
        page = 1
        while True:
            print(f'\n== Crawling page {page} ==')
            soup = BeautifulSoup(requests.get(f'https://kami.im/praed.php?page={page}').text, 'html.parser')

            avatars = soup.find_all(class_='avatar')
            names = soup.find_all(class_='name')
            articles = soup.find_all(class_='article')
            dates = soup.find_all(class_='date')

            if len(names) == 0:
                raise Exception('no messages found')

            for i in range(len(names)):
                # <div class="avatar" style="background-image:url(/getavatar.php?uid=45)"></div>
                avatarURL = avatars[i]['style'].split('url(')[1].split(')')[0]
                uid = avatars[i]['style'].split('uid=')[1].split(')')[0]
                # <span class="name">MadoHomu</span>
                name = names[i].text
                # <div class="article">圆神保佑~</div>
                article = articles[i].text
                # <div class="date">
                #   <b class="floor">#35690</b>
                #   02-Jan-2024 01:39
                # </div>
                id = int(dates[i].b.text[1:])
                date = dates[i].b.next_sibling.text
                # there is a space after the time if followed by "<b>(FROM THE IPHONE/IPAD)</b>", so we need to trim the whitespace
                timestamp = int(datetime.strptime(date.strip(), '%d-%b-%Y %H:%M').timestamp())
                print(uid, name, article, id, date)

                if str(id) in msgs or int(id) in msgs:
                    raise Exception('ID exists')

                msgs[id] = {}
                msgs[id]['name'] = name
                msgs[id]['uid'] = uid
                msgs[id]['article'] = article
                msgs[id]['date'] = date
                msgs[id]['timestamp'] = timestamp

            page += 1

    # catch any errors and save the crawled data
    except (KeyboardInterrupt, Exception) as e:
        traceback.print_exc()

        if len(msgs) > len0:
            msgs = dict(sorted(msgs.items(), key=lambda k: int(k[0])))

            with open('kami.json', 'w', encoding='utf-8') as f:
                json.dump(msgs, f, indent=2, ensure_ascii=False)

            print(f'\n=== Successfully crawled to page {page} ===')
            print(f'Added {len(msgs) - len0} new messages')

        else:
            print('\n=== JSON already up to date ===')

    db = sqlite3.connect('main.db')
    cur = db.cursor()

    cur.execute('SELECT max(id) FROM kami')
    maxid = cur.fetchone()[0]

    maxJsonId = max(list(map(int, list(msgs.keys()))))

    print(f'DB: {maxid}  JSON: {maxJsonId}\n')

    if (maxid < maxJsonId):
        for id in msgs:
            if int(id) > maxid:
                print(f"Inserting {id} ({msgs[id]['name']}) {msgs[id]['article']}")
                cur.execute('INSERT INTO kami VALUES (?, ?, ?, ?, ?, ?, ?)', (id, msgs[id]['timestamp'], msgs[id]['name'], msgs[id]['uid'], msgs[id]['article'], None, None))
        db.commit()

    db.close()

    print('Sleeping...')
    sleep(60 * 5)
