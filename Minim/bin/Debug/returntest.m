String testfunc
	^ "Hello, world."
;

void testfunc2
	Print("Hey - I'm testing a return type without an expression to return.")
	^
;

void main
	a = testfunc
	testfunc2
	Print(a)
;