if (i<height-1 && j > 0 && isT(src, bmS, i+1, j - 1))
{
    aux = getByte(src, bmS, i+1, j-1);
    auxDest = getByte(dest, bmD, ++i, --j);
}
if (i<height-1 && j > 0 && isT(src, bmS, i+1, j))
{
    aux = getByte(src, bmS, i+1, j);
    auxDest = getByte(dest, bmD, ++i, j);
}
if (i<height-1 && j<width-1 && isT(src, bmS, i+1, j+1)) {
    aux = getByte(src, bmS, i+1, j+1);
    auxDest = getByte(dest, bmD, ++i, ++j);
}


if (i>0 && j<width-1 && isT(src, bmS, i-1, j+1)) {
    aux = getByte(src, bmS, i-1, j+1);
    auxDest = getByte(dest, bmD, --i, ++j);
}
if(i>0 && isT(src, bmS, i-1, j)){
    aux = getByte(src, bmS, i-1, j);
    auxDest = getByte(dest, bmD, --i, j);
}
if (i>0 && j>0 && isT(src, bmS, i-1, j-1)) {
    aux = getByte(src, bmS, i-1, j-1);
    auxDest = getByte(dest, bmD, --i, --j);
}

if (j<width-1 && isT(src, bmS, i, j+1)) {
    aux = getByte(src, bmS, i, j+1);
    auxDest = getByte(dest, bmD, i, ++j);
}
if (j>0 && isT(src, bmS, i, j-1)) {
    aux = getByte(src, bmS, i, j-1);
    auxDest = getByte(dest, bmD, i, --j);
}