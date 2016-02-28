#!/usr/bin/env python
import subprocess as sp
from subprocess import PIPE
import socket


def run_recog(filename):
	#filename = '12_7_sw_test_1.txt'
	command = 'sh _run_recognizer.sh ' + filename
	sp.Popen(command, shell=True, executable="/bin/bash")

	x = sp.Popen('sed -n \'3p\' ./testsets/zresult_'+filename+' | cut -d\' \' -f3', stdout=PIPE, shell=True, executable="/bin/bash")
	y = x.communicate()
	print y[0]
	result = y[0]
	return result


TCP_IP = '143.215.199.231'
TCP_PORT = 5005
BUFFER_SIZE = 1450  # Normally 1024, but we want fast response

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind((TCP_IP, TCP_PORT))
s.listen(1)

conn, addr = s.accept()
print 'Connection address:', addr
print 'Connection variable', conn
count = 1 #........................................SET BACK TO 1 
path = "./testsets/"
f = None
record = False
start_msg = False
phrase = ""
frame = 1
curr_filename = ""
while 1:
    data = conn.recv(BUFFER_SIZE)
    if not data: break
    print "received data:", frame, len(data)
    conn.send("ack "+str(frame))  # echo
    frame = frame+1

    if data == "end":
        record = False
        phrase = ""
        f.close()
        print "End End End End"
        recog = run_recog(curr_filename)
        conn.send("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ "+recog)
        curr_filename = ""

    if data=="exit": break

    if record:
        f.write(data)
        f.write("\n")

#    joints = data.split('|')        
#    if len(joints)>0 and record:
#       for joint in joints:
#           #coord = joints(k).split(',')
#           f.write(data)

    if start_msg:
        record = True
        start_msg = False
        phrase = data
        print "SSSSSSSSSSSSSSSSSSSSSS"
        curr_filename = 'z_tester_'+phrase+"_"+str(count)+".txt"
        f = open(path+curr_filename, "w")
        count = count + 1

    if data == "start":
        start_msg = True

conn.close()