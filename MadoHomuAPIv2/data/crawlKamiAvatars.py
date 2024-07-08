import json
import requests
from concurrent.futures import ThreadPoolExecutor


def dl(uid):
    try:
        data = requests.get(f'https://kami.im/getavatar.php?uid={uid}').content
        if data[:5] == b'<?xml':
            print(f'{uid:<4} skipped')
            return
        with open(f'kami_avatars/{uid}.jpg', 'wb') as f:
            f.write(data)
        print(f'{uid:<4} downloaded')
    except:
        print(f'{uid:<4} ERROR!')


if __name__ == '__main__':

    with open('kami1.json', encoding='utf-8') as f:
        kami = json.load(f)

    uids = []

    for id in kami:
        if not kami[id]['uid'] in uids:
            uids.append(kami[id]['uid'])

    print(uids)

    with ThreadPoolExecutor(20) as pool:
        pool.map(dl, uids)
