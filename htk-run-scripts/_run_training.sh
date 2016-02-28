chmod -R 777 *
echo "set all folder permissions to rwx .........."
rm -rf ./ext/*
echo "removed unwanted files from ext folder ....."
find data/* -type d > datadirs
echo "creation of datadirs completed ............."
find data/* -type f > datafiles
echo "creation of datafiles completed ............"
./scripts/gen_commands.sh datadirs > commands
echo "creation of commands completed ............."
./scripts/gen_mlf.sh datafiles ext > labels.mlf
echo "created a new labels.mlf file .............."
sed -i '1 s/.*/#!MLF!#/' labels.mlf
echo "fixed any errors is labels.mlf 1st entry ..."
sudo ./scripts/train.sh scripts/options.sh  2>&1 > out.log
echo "initial training of model completed ........"
