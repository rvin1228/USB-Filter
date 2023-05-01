import os, shutil, hashlib, inotify.adapters, datetime, re, psutil
 
#getting the MD5 hash value of a file
def get_md5_hash(file_path):
    md5_hash = hashlib.md5()
    with open(file_path, 'rb') as file:
        for chunk in iter(lambda: file.read(4096), b''):
            md5_hash.update(chunk)
    return md5_hash.hexdigest()
 
 
#monitoring of file transfers between flash drive and folder
def monitor_file_transfer(src_path, dest_path, qua_path):
 
    i = inotify.adapters.Inotify() #use i to initialize monitoring of file system
 
    drives = os.listdir('/media/raspberry')
    for drive in drives:
        src_path = os.path.join('/media/raspberry', drive)
        if os.path.isdir(src_path):
            i.add_watch(src_path)
        print(drive)
 
    i.add_watch(dest_path) #monitor destination path
 
    #provide information for file system events
    for event in i.event_gen(yield_nones=False):
        (_, type_names, path, filename) = event
 
        if 'IN_CLOSE_WRITE' in type_names: #will monitor changes to files after file being written
            file_path = os.path.join(path, filename)
 
            #scan .exe files
            if filename.endswith('.exe'):
                with open(file_path, 'r', encoding = "ISO-8859-1") as f: #scan PoC file
                    unicode_string = f.read()
                    index = unicode_string.find("MALWARE ITO")
                    MZ = unicode_string.find("MZ")
                    PE = unicode_string.find("PE")
 
                existHash = "84c82835a5d21bbcf75a61706d8ab549" #MD5 hash value of WannaCry
                md5_hash = get_md5_hash(file_path) #get MD5 hash value of file
 
                with open("info.txt", "r") as file: #for historical data information
                    file_contents = file.read()
                malwareCount = int(re.search(r"Malware Count: (\d+)", file_contents).group(1))
                safeCount = int(re.search(r"Safe Count: (\d+)", file_contents).group(1))
 
                data = open("data.txt", "a") #append details to output file
                data.write("\nFilename: " + str(filename))
                data.write("\nFile Path: " + file_path)
                file_size = os.path.getsize(file_path)
                data.write("\nFile Size: " + str(file_size) + " bytes")
                data.write("\nMD5 Hash Value: " + md5_hash)
                now = datetime.datetime.now()
                data.write(f"\nDate Detected: {now.strftime('%Y-%m-%d %H:%M:%S')}")
 
                #check MZPE file structure and compare MD5 hash value
                if PE > index > MZ or existHash == md5_hash:
                    status = "Quarantine"
                    print(f"Blocking transfer of {filename}")
 
                    qua_filename = filename.replace('.exe','.exe.lock') #rename file
                    qua_file_path = os.path.join(qua_path, qua_filename)
                    shutil.move(file_path, qua_file_path) #quarantine file to quarantine folder
                    print(f"MD5 hash of {filename}: {md5_hash}")
                    malwareCount += 1
 
                else:
                    status = "Safe"
                    print(f"File transfer allowed: {filename}")
                    print(f"MD5 hash of {filename}: {md5_hash}")
                    safeCount += 1
 
                data.write("\nStatus: " + status + "\n")
                data.close()
                file_contents = re.sub(r"Malware Count: \d+", f"Malware Count: {malwareCount}", file_contents)
                file_contents = re.sub(r"Safe Count: \d+", f"Safe Count: {safeCount}", file_contents)
                with open("info.txt", "w") as file:
                    file.write(file_contents)
 
if __name__ == "__main__":
    src_path = None
    dest_path = "/home/raspberry/Desktop"
    qua_path = "/home/raspberry/Desktop/Quarantine"
 
    monitor_file_transfer(src_path, dest_path, qua_path)
