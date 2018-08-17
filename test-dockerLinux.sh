unameOut="$(uname -s)"

docker build -t issueDemonstration:latest CultureIssueDemonstration

if [[ $unameOut == Cygwin* ]] || [[ $unameOut == MINGW* ]]
	then
		winpty docker run -it issueDemonstration:latest
	else
		docker run -it issueDemonstration:latest
fi
