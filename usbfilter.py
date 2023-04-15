import io, os, shutil, sys, hashlib
 
src = ("/media/raspberry/") #usb drive source
dst = ("/home/raspberry/Desktop/Scanned Files/") #filtered folder destination
cwd = os.getcwd() #current working directory
existHash = "84c82835a5d21bbcf75a61706d8ab549" #MD5 hash value for Wannacry Ransomware
 
if not os.path.exists(dst):
    os.makedirs(dst)
 
for root, dirs, files in os.walk(src):
    for file in files:
        file_path = os.path.join(cwd, root, file)
        filename = os.path.basename(file_path)
 
        #For getting MD5 hash value of the .exe file
        with open(file_path, 'rb') as myexe:
            file_content = myexe.read()
 
        #Convert .exe to unicode string
        with open(file_path, "r", encoding = "ISO-8859-1") as f:
            src_path = os.path.join(root, file)
            dst_path = src_path.replace(src, dst)
            if not os.path.exists(os.path.dirname(dst_path)):
                os.makedirs(os.path.dirname(dst_path))
            shutil.copy2(src_path, dst_path)
 
            if file_path.endswith('.exe'): #check file extension
                unicode_string = f.read()
                index = unicode_string.find("MALWARE ITO") #locate position of identifier
                MZ = unicode_string.find("MZ") #determine MS-DOS EXE format
                PE = unicode_string.find("PE") #locate end of PE header
 
                data = open("data.txt", "a") #append details to output file
                data.write("\nFilename: " + str(filename))
 
                md5_hash = hashlib.md5(file_content).hexdigest() #get MD5 hash value of the .exe file
                data.write("\nMD5 Hash Value: " + md5_hash)
 
                if PE > index > MZ or existHash == md5_hash:
                    malware = True
                    if existHash == md5_hash:
                        exist = True
                        malware = False
                else:
                    malware = False
                    exist = False
 
                for dirpath, dirnames, filenames in os.walk(dst): #get file path
                        for fname in filenames:
                            if fname == filename:
                                path = os.path.abspath(os.path.join(dirpath, fname))
                                fldr, filename = os.path.split(path)
                                rfldr = os.path.relpath(fldr, dst)
                                scanned_file_path = os.path.join(rfldr, filename)
                                break
 
                if malware:
                    data.write("\nIs malware: YES")
                    data.write("\nFile Path: " + scanned_file_path + "\n")
                elif exist:
                    data.write("\nIs malware: Yes (Identified as Wannacry Ransomware)")
                    data.write("\nFile Path: " + scanned_file_path + "\n")
                else:
                    data.write("\nIs malware: NO")
                    data.write("\nFile Path: " + scanned_file_path + "\n")
 
                data.close()
 
# copy the data.txt file to the filtered folder
shutil.copy2(os.path.join(cwd, "data.txt"), os.path.join(dst, "data.txt"))
