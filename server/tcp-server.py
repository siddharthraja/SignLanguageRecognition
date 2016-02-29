#!/usr/bin/env python

import socket
import os

TCP_IP = 'localhost'
TCP_PORT = 5005
BUFFER_SIZE = 1450  # Normally 1024, but we want fast response

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind((TCP_IP, TCP_PORT))
s.listen(1)

conn, addr = s.accept()
print 'Connection address:', addr
print 'Connection variable', conn
count = 19#........................................SET BACK TO 1 
path = "./data_feed/"
fullpath = ""
f = None
record = False
start_msg = False
delete_recording = False
new_phrase = False
folder = ""
phrase = ""
frame = 1
file_name = ""

while 1:
	data = conn.recv(BUFFER_SIZE)
	if not data: break
	print "received data:", frame, len(data)
	conn.send("ack "+str(frame))  # echo
	frame = frame+1

	if data == "end":
		record = False
		#phrase = ""
		f.close()
		print "End End End End End End End End"

	if data=="exit": break

	if record:
		f.write(data)
		f.write("\n")

	if new_phrase:
		new_phrase = False
		print "New New New New New New New New"
		phrase = data
		folder = ""+phrase+"/"
		directory = path+folder
		if not os.path.exists(directory):
			os.makedirs(directory)
		count = 1


	#start of each phrase sample
	if start_msg:
		record = True
		start_msg = False
		phrase = data
		print "Start Start Start Start Start Start"
		file_name = path+folder+phrase+"_"+str(count)+".txt"
		f = open(path+folder+phrase+"_"+str(count)+".txt", "w")
		count = count + 1

	if delete_recording and count > 1:
		record = False
		delete_recording = False
		f.close()
		count -= 1
		file_to_del = path+folder+phrase+"_"+str(count)+".txt"        
		os.remove(file_to_del)
		print "Deleted Deleted Deleted Deleted Deleted Deleted Deleted"

	if data == "start":
		start_msg = True

	if data == "delete":
		delete_recording = True
		
	if data == "new_phrase":
		new_phrase = True

conn.close()
