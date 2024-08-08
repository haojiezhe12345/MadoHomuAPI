import requests
from threading import Thread
import base64
import json
import os
from concurrent.futures import ThreadPoolExecutor
from time import perf_counter, sleep

# url = 'https://haojiezhe12345.top/madohomu/api/post'
# url = '192.168.2.99:8001/test/post'
# url = 'http://localhost:5213/upload'


# x');DELETE FROM comments--
# x');SELECT * FROM comments--

session = requests.session()
baseurl = 'http://localhost:5213'


def get(i):
    r = session.get(f'{baseurl}/comments?from={i}&count=50')
    try:
        json.loads(r.text)
        return True
    except:
        return r


def post(i, sender=None, comment=None):
    r = session.post(f'{baseurl}/post', json={
        'sender': sender or f'testuser{i}\'s',
        'comment': comment or "x');DELETE FROM comments--"
    })
    if r.text == '1':
        return True
    else:
        return r


def login(i=1, name=None, email=None):
    r = session.post(f'{baseurl}/user/login', json={
        'name': name or f'string{i}',
        'email': email,
    })
    try:
        x = json.loads(r.text)
        if x['code'] == 1:
            return True
        else:
            raise Exception()
    except:
        return r


def register(i=1, name=None, email=None):
    r = session.post(f'{baseurl}/user/register', json={
        'name': name or f'string{i}',
        'email': email,
    })
    try:
        x = json.loads(r.text)
        if x['code'] == 1:
            return True
        else:
            raise Exception()
    except:
        return r


def changeEmail(i=1, data=None):
    r = session.put(f'{baseurl}/user/update', json={
        'email': data,
    })
    return r


def changeName(i=1, data=None):
    r = session.put(f'{baseurl}/user/update', json={
        'name': data,
    })
    return r


def uploadAvatar(i=1, image=R"C:\Users\31126\OneDrive\Avatars\madoka.jpg"):
    with open(image, 'rb') as f:
        r = session.put(f'{baseurl}/user/update', json={
            'avatar': base64.b64encode(f.read()).decode('ascii'),
        })
    print(r.text)


def userMe(i=1):
    r = session.get(f'{baseurl}/user/me')
    print(r.text)


def postFile(i):
    url = f'{baseurl}/upload'
    f = open(R"C:\Users\31126\Desktop\magireco.png", mode='rb')
    # data = f.read()
    # f.close()
    x = requests.post(url, files={
        '.sasd': ('test.png', f)
    })
    print(i, end=': ')
    print(x.text)


def postWithImg(i):
    url = f'{baseurl}/post'
    with open(R"C:\Users\31126\Desktop\magireco.png", "rb") as image_file:
        encoded_string = base64.b64encode(image_file.read()).decode('ascii')
    x = requests.post(url, json={
        'sender': f'testuser{i}\'s',
        'comment': "x');DELETE FROM comments--",
        'images': [
            encoded_string,
            encoded_string,
            encoded_string,
            # '????'
        ]
    })
    print(i, end=': ')
    print(x.text)


def postLargeFile():
    url = f'{baseurl}/post'
    with open(R"D:\魔法少女まどか☆マギカ\Mahou Shoujo Madoka Magika - 10 (BD 1280x720)-muxed.mp4", "rb") as image_file:
        encoded_string = base64.b64encode(image_file.read()).decode('ascii')
    x = requests.post(url, json={
        'sender': f'test',
        'comment': "very large file",
        'images': [
            encoded_string,
            encoded_string,
        ]
    })
    print(x.text)


def stress(fn, count):
    created = 0
    success = 0
    fail = 0
    running = True

    def send(x):
        nonlocal created, success, fail
        created += 1
        r = fn(x)
        if r == True:
            success += 1
        else:
            fail += 1
            print(f'\n{r.status_code}: {r.text}')
            printStat()

    def printStat():
        print(f'Created: {created}/{count}, Success: {success}/{count}, Failed: {fail}        ', end='\r')

    def show():
        while running:
            printStat()
            sleep(0.1)
        printStat()
        print()

    counter = Thread(target=show)
    counter.start()

    t1 = perf_counter()
    with ThreadPoolExecutor(max_workers=99999) as exe:
        exe.map(send, range(0, count))
    t = perf_counter() - t1

    running = False
    counter.join()

    print(f'\nSent {count} requests in {t:.2f} seconds\nSuccess rate: {success / t:.1f}/s  Total rate: {count / t:.1f}/s')


if __name__ == "__main__":
    # for i in range(0, 20):
    #    print()

    baseurl = 'http://192.168.2.99:8001/api'
    # stress(post, 3000)

    session.headers['token'] = '1d4a0962-b06d-496e-b327-f994c0763d3b'
    # stress(post, 10000)
    # stress(get, 10000)
    stress(login, 10000)
    # stress(register, 10000)
    # register(1, 'string2')
    # userMe()
    # changeEmail(1, "string")
    # print(changeEmail(1, os.urandom(4).hex()).text)
    # print(changeName(1, 'string').text)
    # print(changeName(1, R'" ~`!@#$%^&*()_+-={}[]:;"\'|\<>,.?/ " ' + os.urandom(2).hex()).text)
    # uploadAvatar()
    # print(post(1, 'awa', "??????????'''''"))
    # postLargeFile()
