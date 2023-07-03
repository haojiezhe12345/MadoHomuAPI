import json
import sqlite3

f = open('kami.json', encoding='utf-8')
kami = json.load(f)
f.close()

kami = sorted(kami, key=lambda d: d['timestamp']) 

#print(kami)
#json_object = json.dumps(kami, indent=4, ensure_ascii=False)
#with open("kami_sorted.json", "w", encoding='utf-8') as outfile:
#    outfile.write(json_object)


conn = sqlite3.connect('main_with_kami.db')
cursor = conn.cursor()  

id = 0 - len(kami)
#print(id)

#testtxt = "shi jian di da di da , jiu xiao shi le ne.\ner ni:HJY ,whrer are you ?\nni shi fou hai ji de wo ne ?\nit\""
#cursor.execute(f"INSERT INTO comments (id, time, sender, comment) VALUES (999, 0, 'test', '{testtxt}')")

cursor.execute('DELETE FROM comments')

for comment in kami:
    time = comment['timestamp']
    sender = comment['name'].replace("'", "''").replace('\u0000', '')
    comment = comment['article'].replace("'", "''").replace('\u0000', '')
    #print(comment)
    cursor.execute(f"INSERT INTO comments (id, time, sender, comment) VALUES ({id}, {time}, '{sender}', '{comment}')")
    id += 1


cursor.execute(f"INSERT INTO comments (id, time, sender, comment) VALUES (0, 0, '浩劫者12345', '以上为kami.im建站以来到22年3月的消息记录\n\n感谢Discord频友Yuudachi_Kai2备份的数据!')")

conn.commit()

conn.close()

