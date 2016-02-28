#!/bin/bash
#echo $1
./scripts/recognize.sh ./testsets/$1 ./testsets/zresult_$1 ./scripts/options.sh ./models/hmm0.3/newMacros
