import requests
from threading import Thread
import base64
import json

# url = 'https://haojiezhe12345.top/madohomu/api/post'
# url = '192.168.2.99:8001/test/post'
# url = 'http://localhost:5017/upload'


# x');DELETE FROM comments--
# x');SELECT * FROM comments--

def get(i):
    x = requests.get('http://localhost:5017/comments?from=-20000&count=10')
    print(f'\n\n============ {i}\n\n{x.text}')
    # response = json.loads(x.text)
    # if response[0]['sender'] != "浩劫者12345":
    #    print('error', end='')
    # print()


def post(i):
    url = 'http://localhost:5017/post'
    x = requests.post(url, json={
        'sender': f'testuser{i}\'s',
        'comment': "x');DELETE FROM comments--"
    })
    print(i, end=' ')
    # if x.text != "1":
    #    print('error', end='')
    print(x.text)


def postFile(i):
    url = 'http://localhost:5017/upload'
    f = open(R"C:\Users\31126\Desktop\magireco.png", mode='rb')
    # data = f.read()
    # f.close()
    x = requests.post(url, files={
        '.sasd': ('test.png', f)
    })
    print(i, end=': ')
    print(x.text)


def postWithImg(i):
    url = 'http://localhost:5017/post'
    with open(R"C:\Users\31126\Desktop\magireco.png", "rb") as image_file:
        encoded_string = base64.b64encode(image_file.read()).decode('ascii')
    x = requests.post(url, json={
        'sender': f'testuser{i}\'s',
        'comment': "x');DELETE FROM comments--",
        'images': [
            encoded_string,
            encoded_string,
            encoded_string,
            #'????'
        ]
    })
    print(i, end=': ')
    print(x.text)


if __name__ == "__main__":
    # for i in range(0, 20):
    #    print()

    for i in range(0, 1000):
        thread = Thread(target=post, args=[i])
        thread.start()
