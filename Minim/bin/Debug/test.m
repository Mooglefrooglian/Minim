void testfunc(String a)
	Print("Testing a function call! \n\nThe next line should read \"Hello world from a function!\"")
	Print(a)
;

void main
	a = "Hello, world"
	b = a
	c = b
	d = "Hello world from a function!"
	Print(c)
	Print("I am a new language but I am going to quickly learn new features.")
	Print("I am a third line!")
	testfunc(d)
;