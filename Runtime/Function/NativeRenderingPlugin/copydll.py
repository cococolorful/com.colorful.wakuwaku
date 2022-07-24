import os
import shutil

source = 'C:/Users/hgx/wakuwaku/Assets/Plugins/x86-64/NativeRenderer.dll'
target = 'C:/Users/hgx/wakuwaku/bin/asdasdas/wakuwaku_Data/Plugins/x86_64'

try:
   shutil.copy(source, target)
except IOError as e:
   print("Unable to copy file. %s" % e)
except:
   print("Unexpected error:", sys.exc_info())