import os, sys, shutil
 
def main():
    if len(sys.argv) != 3:
        #usage of python script through terminal
        print("Usage: python restore.py <destination folder> <specific_filename>")
        sys.exit(1)
 
    src_folder = "/home/admin/Quarantine"
    dest_folder = sys.argv[1]
    specific_filename = sys.argv[2] + ".exe.quarantine"
 
    for filename in os.listdir(src_folder): #find location of file
        if filename == specific_filename:
            src_path = os.path.join(src_folder, filename)
            move_path = os.path.join(dest_folder, filename)
            shutil.move(src_path, move_path)
            
            new_filename = filename[:-11] #remove the .quarantine file extension
            dest_path = os.path.join(dest_folder, new_filename)
            os.rename(move_path, dest_path) #revert back to specified destination folder
            print(f"Restored {new_filename}")
 
if __name__ == "__main__":
    main()
