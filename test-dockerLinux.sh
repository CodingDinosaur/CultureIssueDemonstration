unameOut="$(uname -s)"

docker build -t issuedemonstration:latest CultureIssueDemonstration

if [[ $unameOut == Cygwin* ]] || [[ $unameOut == MINGW* ]]
	then
		winpty docker run -it issuedemonstration:latest
	else
		docker run -it issuedemonstration:latest
fi
