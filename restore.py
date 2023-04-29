import os, sys
 
def main():
    if len(sys.argv) != 3:
        #usage of python script through terminal
        print("Usage: python restore.py <destination folder> <specific_filename>")
        sys.exit(1)
 
    src_folder = "/home/raspberry/Desktop/Quarantine"
    dest_folder = sys.argv[1]
    specific_filename = sys.argv[2] + ".exe.lock"
 
    for filename in os.listdir(src_folder): #find location of file
        if filename == specific_filename:
            new_filename = filename[:-5] #remove the .lock file extension
            src_path = os.path.join(src_folder, filename)
            dest_path = os.path.join(dest_folder, new_filename)
            os.rename(src_path, dest_path) #revert back to specified destination folder
            print(f"Restored {new_filename}")
 
if __name__ == "__main__":
    main()