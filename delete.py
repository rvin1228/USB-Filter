import os, sys
 
def main():
    if len(sys.argv) != 2:
        #usage of python script through terminal
        print("Usage: python delete.py <specific_filename>")
        sys.exit(1)
 
    src_folder = "/home/raspberry/Desktop/Quarantine"
    specific_filename = sys.argv[1] + ".exe.lock"
 
    for filename in os.listdir(src_folder): #find location of file
        if filename == specific_filename:
            src_path = os.path.join(src_folder, filename)
            os.remove(src_path) #delete file permanently
            print(f"Deleted {src_path}")
 
if __name__ == "__main__":
    main()