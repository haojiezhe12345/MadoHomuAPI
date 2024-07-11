import requests
from threading import Thread
import base64
import json

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
        print(r.text)
        return False


def post(i):
    r = session.post(f'{baseurl}/post', json={
        'sender': f'testuser{i}\'s',
        'comment': "x');DELETE FROM comments--"
    })
    if r.text == '1':
        return True
    else:
        print(r.text)
        return False


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


def stress(fn, count):
    created = 0
    success = 0
    fail = 0

    def send(x):
        nonlocal created, success, fail
        created += 1
        succeed = fn(x)
        if succeed:
            success += 1
        else:
            fail += 1
        print(f'Created: {created}/{count}, Success: {success}/{count}, Failed: {fail}        ', end='\r')

    for i in range(0, count):
        thread = Thread(target=send, args=[i])
        thread.start()


if __name__ == "__main__":
    # for i in range(0, 20):
    #    print()

    # baseurl = 'http://192.168.2.99:8001/api'
    stress(post, 3000)
