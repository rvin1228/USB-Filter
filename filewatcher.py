import os, shutil, hashlib, inotify.adapters
 
#getting the MD5 hash value of a file
def get_md5_hash(file_path):
    md5_hash = hashlib.md5()
    with open(file_path, 'rb') as file:
        for chunk in iter(lambda: file.read(4096), b''):
            md5_hash.update(chunk)
    return md5_hash.hexdigest()
 
 
#monitoring of file transfers between flash drive and folder
def monitor_file_transfer(src_path, dest_path, qua_path):
    exclude_files = ["safe.exe", "trusted.exe"] #for exclusion of files
 
    i = inotify.adapters.Inotify() #use i to initialize monitoring of file system
 
    i.add_watch(src_path) #monitor source path
    i.add_watch(dest_path) #monitor destination path
 
 
    #provide information for file system events
    for event in i.event_gen(yield_nones=False):
        (_, type_names, path, filename) = event
 
        if 'IN_CLOSE_WRITE' in type_names: #will monitor changes to files after file being written
            file_path = os.path.join(path, filename)
 
            #scan .exe files, exclude filenames listed at exclude_files
            if filename.endswith('.exe') and filename not in exclude_files:
                with open(file_path, 'r', encoding = "ISO-8859-1") as f: #scan PoC file
                    unicode_string = f.read()
                    index = unicode_string.find("MALWARE ITO")
                    MZ = unicode_string.find("MZ")
                    PE = unicode_string.find("PE")
 
                existHash = "84c82835a5d21bbcf75a61706d8ab549" #MD5 hash value of WannaCry
                md5_hash = get_md5_hash(file_path) #get MD5 hash value of file
 
                #check MZPE file structure and compare MD5 hash value
                if PE > index > MZ or existHash == md5_hash:
                    print(f"Blocking transfer of {filename}")
 
                    qua_filename = filename.replace('.exe','.exe.lock') #rename file
                    qua_file_path = os.path.join(qua_path, qua_filename)
                    shutil.move(file_path, qua_file_path) #quarantine file to quarantine folder
                    print(f"MD5 hash of {filename}: {md5_hash}")
                else:
                    print(f"File transfer allowed: {filename}")
                    print(f"MD5 hash of {filename}: {md5_hash}")
 
if __name__ == "__main__":
    src_path = "/media/raspberry/VIN"
    dest_path = "/home/raspberry/Desktop/folder"
    qua_path = "/home/raspberry/Desktop/Quarantine"
 
    monitor_file_transfer(src_path, dest_path, qua_path)