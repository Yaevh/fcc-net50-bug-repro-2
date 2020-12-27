if [ `ps -u $USER | grep -i Szlem1 | wc -l` -lt 1 ]
then
    echo 'Starting SZLEM.WebUI...'
	DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
	cd $DIR
    ~/webapps/szlem/Szlem.WebUI $@;
else
    echo 'SZLEM.WebUI is running'
fi