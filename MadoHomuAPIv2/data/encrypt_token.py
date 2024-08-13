import json
import sqlite3

import hashlib
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad, unpad
from base64 import b64encode, b64decode

with open("config_location.txt") as f:
    with open(f.read()) as f1:
        ENCRYPTION_KEY = json.load(f1)['EncryptionKey']


def sha256_hash_with_salt(input_str, bits_length=256, salt=None):
    if salt is None:
        salt = ENCRYPTION_KEY
    hash = hashlib.sha256((input_str + salt).encode('utf-8')).digest()
    if bits_length > len(hash) * 8:
        raise Exception(f"The maximum hash length is {len(hash) * 8}, but {bits_length} requested")
    elif bits_length == len(hash) * 8:
        return hash
    else:
        return hash[:bits_length // 8]


def encrypt_string(txt, key=None, iv=None):
    if key is None:
        key = ENCRYPTION_KEY
    if iv is None:
        iv = ENCRYPTION_KEY

    aes_key = sha256_hash_with_salt(key, 256)
    aes_iv = sha256_hash_with_salt(iv, 128)

    cipher = AES.new(aes_key, AES.MODE_CBC, aes_iv)
    encrypted_bytes = cipher.encrypt(pad(txt.encode('utf-8'), AES.block_size))

    return b64encode(encrypted_bytes).decode('utf-8')


def decrypt_string(encrypted_base64, key=None, iv=None, salt=None):
    try:
        if key is None:
            key = ENCRYPTION_KEY
        if iv is None:
            iv = ENCRYPTION_KEY

        aes_key = sha256_hash_with_salt(key, 256, salt)
        aes_iv = sha256_hash_with_salt(iv, 128, salt)

        cipher = AES.new(aes_key, AES.MODE_CBC, aes_iv)
        encrypted_bytes = b64decode(encrypted_base64)
        decrypted_bytes = unpad(cipher.decrypt(encrypted_bytes), AES.block_size)

        return decrypted_bytes.decode('utf-8')
    except Exception as e:
        print(f"Failed to decrypt string '{encrypted_base64}', reason:\n{e}")
        return encrypted_base64


if __name__ == '__main__':
    db = sqlite3.connect(R"main.db")
    cur = db.cursor()

    for row in cur.execute("SELECT * FROM users"):
        cur1 = db.cursor()
        id = row[0]

        token_raw = row[5]
        if token_raw and len(token_raw) == 36 and token_raw.count('-') == 4:
            token_encrypted = encrypt_string(token_raw)
            cur1.execute('UPDATE users SET token = ? where id = ?', (token_encrypted, id))
            print(f'Encrypted token (id={id}): {token_raw} -> {token_encrypted}')

        email_encrypted_nosalt = row[3]
        if email_encrypted_nosalt:
            email_raw = decrypt_string(email_encrypted_nosalt, salt='')
            email_encrypted = encrypt_string(email_raw)
            cur1.execute('UPDATE users SET email = ? where id = ?', (email_encrypted, id))
            print(f'Re-encrypted email (id={id}): {email_encrypted_nosalt} -> {email_raw} -> {email_encrypted}')

    db.commit()
    db.close()
