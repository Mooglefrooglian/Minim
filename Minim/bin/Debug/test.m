void testfunc(String a)
	print("Testing a function call! The next line should read \"Hello world from a function!\"")
	print(a)
;

void main
	a = "Hello, world"
	b = a
	c = b
	d = "Hello world from a function!"
	print(c)
	print("I am a new language but I am going to quickly learn new features.")
	print("I am a third line!")
	testfunc(d)
;