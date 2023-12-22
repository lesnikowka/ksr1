from matplotlib import pyplot as plt
import numpy as np
import sqlite3
import sys

p = 2
max_der = 10**5
frigid_prec = 10**-5

xi = []
v1i = []
v2i = []
v12i = []
v22i = []
olp1i = []
olp2i = []
c1i = []
c2i = []
hi = []

WC = True
x0 = 0
u10 = 7
u20 = 13
Eb = 0.001
E = 0.0001
bound = 1000
h_ = 0.001
N = 1000000

def catchParsFromCmd():
    global x0, u10, u20, Eb, E, bound, h_, N, WC

    if len(sys.argv) == 1:
        print("программа запущена со стандартными параметрами")
        return

    x0 = float(sys.argv[1])
    u10 = float(sys.argv[2])
    h_ = float(sys.argv[3])
    N = int(sys.argv[4])
    E = float(sys.argv[5])
    Eb = float(sys.argv[6])
    WC = bool(int(sys.argv[7]))
    #A = float(sys.argv[8])
    #B = float(sys.argv[9])
    #C = float(sys.argv[10])
    #taskType = sys.argv[11]
    bound = float(sys.argv[8])
    u20 = float(sys.argv[9])

def savetodb():
    connection = sqlite3.connect("../database/lab1.sqlite3")
    cursor = connection.cursor()

    for i in range(len(xi)):
        cursor.executemany("insert into main2 values(?,?,?,?,?,?,?,?,?,?,?,?,?)",
                            [[v2i[i], x0, u10, i, xi[i], v1i[i], v12i[i], abs(v1i[i]-v12i[i]), olp1i[i],
                            hi[i], c1i[i], c2i[i], u20]])

        cursor.executemany("insert into main2der values(?,?,?,?,?,?,?,?,?,?,?,?,?)",
                            [[v2i[i], x0, u10, i, xi[i], v2i[i], v22i[i], abs(v2i[i]-v22i[i]), olp2i[i],
                            hi[i], c1i[i], c2i[i], u20]])

    connection.commit()

def savevalwc(xi_, v1i_, v2i_, v12i_, v22i_, olp1i_, olp2i_, c1i_, c2i_, hi_):
    global xi, v1i, v2i, v12i, v22i, olp1i, olp2i, c1i, c2i
    xi.append(xi_)
    v1i.append(v1i_)
    v2i.append(v2i_)
    v12i.append(v12i_)
    v22i.append(v22i_)
    olp1i.append(olp1i_)
    olp2i.append(olp2i_)
    c1i.append(c1i_)
    c2i.append(c2i_)
    hi.append(hi_)

def eraseEnd():
    global xi, v1i, v2i, v12i, v22i, olp1i, olp2i, c1i, c2i, hi
    xi.pop()
    v1i.pop()
    v2i.pop()
    v12i.pop()
    v22i.pop()
    olp1i.pop()
    olp2i.pop()
    c1i.pop()
    c2i.pop()
    hi.pop()

def getMatr():
    return [
        [-500.005, 499.995],
        [499.995, -500.005]
        ]

def createfunc1(matr_):
    def func(x, u1, u2):
        return matr_[0][0] * u1 + matr_[0][1] * u2

    return func

def createfunc2(matr_):
    def func(x, u1, u2):
        return matr_[1][0] * u1 + matr_[1][1] * u2

    return func

def step(f1, f2, h, x, v1, v2, wc=False):
    f1n = f1(x, v1, v2)
    f2n = f2(x, v1, v2)
    f1next = f1(x + h, v1 + h * f1n, v2 + h * f2n)
    f2next = f2(x + h, v1 + h * f1n, v2 + h * f2n)
    v1next = v1 + (h / 2) * (f1n + f1next)
    v2next = v2 + (h / 2) * (f2n + f2next)
    x += h
    if not wc:
        xnext_, v1next_, v2next_ = step(f1, f2, h, x - h, v1, v2, True)
        x1half_, v1half_, v2half_ = step(f1, f2, h / 2, x - h, v1, v2, True)
        x12next_, v12next_, v22next_ = step(f1, f2, h / 2, x1half_, v1half_, v2half_, True)
        s1 = abs(v1next_ - v12next_)
        s2 = abs(v2next_ - v22next_)
        s1 /=  (2**p - 1)
        s2 /=  (2**p - 1)

        savevalwc(x, v1next_, v2next_, v12next_, v22next_, s1, s2, 0, 0, h)

    return x, v1next, v2next

def stepWC(f1, f2, h, x, v1, v2, e, maxeabs_):
    xnext, v1next, v2next = step(f1, f2, h, x, v1, v2, True)
    x1half, v1half, v2half  = step(f1, f2, h / 2, x, v1, v2, True)
    x12next, v12next, v22next = step(f1, f2, h / 2, x1half, v1half, v2half, True)
    s1 = abs(v1next - v12next)
    s2 = abs(v2next - v22next)
    s1 /= (2 ** p - 1)
    s2 /= (2 ** p - 1)
    s = max(s1, s2)
    minbound = e / (2**p - 1)
    divisions = 0

    while h >= 2. / maxeabs_ - frigid_prec:
        h /= 2
        divisions += 1
        xnext, v1next, v2next = step(f1, f2, h, x, v1, v2, True)
        x1half, v1half, v2half = step(f1, f2, h / 2, x, v1, v2, True)
        x12next, v12next, v22next = step(f1, f2, h / 2, x1half, v1half, v2half, True)
        s1 = abs(v1next - v12next)
        s2 = abs(v2next - v22next)
        s1 /= (2 ** p - 1)
        s2 /= (2 ** p - 1)
        s = max(s1, s2)

    if s > minbound and s <= e:
        savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 0, h)
        return xnext, v1next, v2next, h
    elif s <= minbound:
        if 2 * h >= 2 / maxeabs_ - frigid_prec:
            savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 0, h)
            return xnext, v1next, v2next, h
        else:
            savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 1, h)
            return xnext, v1next, v2next, 2 * h
    else:
        while s > e:
            h /= 2
            divisions += 1
            xnext, v1next, v2next = step(f1, f2, h, x, v1, v2, True)
            x1half, v1half, v2half = step(f1, f2, h / 2, x, v1, v2, True)
            x12next, v12next, v22next = step(f1, f2, h / 2, x1half, v1half, v2half, True)
            s1 = abs(v1next - v12next)
            s2 = abs(v2next - v22next)
            s1 /= (2 ** p - 1)
            s2 /= (2 ** p - 1)
            s = max(s1, s2)

        savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 0, h)

        return xnext, v1next, v2next, h

def RK4(f1, f2, h, x, v1, v2, n, b, eb):
    savevalwc(x, v1, v2, v1, v2, 0, 0, 0, 0, h)
    oldx = x
    oldv1 = v1
    oldv2 = v2

    for i in range(n):
        x, v1, v2 = step(f1, f2, h, x, v1, v2)
        if f1(x, v1, v2) > max_der or f2(x, v1, v2) > max_der or x >= b - eb and x <= b:
            return
        elif x > b:
            eraseEnd()
            x, v1, v2 = step(f1, f2, b - oldx, oldx, oldv1, oldv2)

def RK4WC(f1, f2, h, x, v1, v2, n, b, eb, e, maxeabs_):
    savevalwc(x, v1, v2, v1, v2, 0, 0, 0, 0, h)

    for i in range(n):
        oldx = x
        oldv1 = v1
        oldv2 = v2

        x, v1, v2, h = stepWC(f1, f2, h, x, v1, v2, e, maxeabs_)
        if f1(x, v1, v2) > max_der or f2(x, v1, v2) > max_der or x >= b - eb and x <= b:
            return
        elif x > b:
            eraseEnd()
            stepWC(f1, f2, b - oldx, oldx, oldv1, oldv2, e, maxeabs_)
            return

def trueSol1(alpha_, x):
    return -alpha_[0] * np.exp(-1000.0 * x) + alpha_[1] * np.exp(-0.01 * x)

def trueSol2(alpha_, x):
    return alpha_[0] * np.exp(-1000.0 * x) + alpha_[1] * np.exp(-0.01 * x)

matr = getMatr()
func1 = createfunc1(matr)
func2 = createfunc2(matr)
val, sup = np.linalg.eig(matr)
maxeabs = max([abs(e) for e in val])
vec1 = [-1, 1]
vec2 = [1, 1]
coefmatr = [[vec1[0],vec2[0]], [vec1[1],vec2[1]]]
alpha = np.linalg.solve(coefmatr, [u10, u20])

catchParsFromCmd()

if WC:
    RK4WC(func1, func2, h_, x0, u10, u20, N, bound, Eb, E, maxeabs)
else:
    RK4(func1, func2, h_, x0, u10, u20, N, bound, Eb)

print(alpha)

savetodb()

plt.figure(figsize=(18,9))

y1 = [trueSol1(alpha, xx) for xx in xi]
y2 = [trueSol2(alpha, xx) for xx in xi]


plt.subplot(131)
plt.title("v1(x)")
plt.plot(xi, v1i, label="Численное решение")
plt.plot(xi, y1,linestyle='--', label="Истинное решение")
plt.legend()

plt.subplot(132)
plt.title("v2(x)")
plt.plot(xi, v2i, label="Численное решение")
plt.plot(xi, y2,linestyle='--', label="Истинное решение")
plt.legend()

plt.subplot(133)
plt.title("v2(v1)")
plt.plot(v1i, v2i, label="Численное решение")
plt.plot(y1, y2, linestyle='--', label="Истинное решение")
plt.legend()

plt.savefig("graph.png")

#plt.show()

infoTextWC = '''
n = %
b - xn = %
Макс. ОЛП = % при x = %
Удвоений: %
Делений: %
Минимальный шаг: % при x = %
Максимальный шаг: % при x = %

Для U2:
Макс. ОЛП = % при x = %

v1n = %
v2n = %
'''

infoText = '''
n = %
b - xn = %
Макс. ОЛП = % при x = %
Для U2:
Макс. ОЛП = % при x = %

v1n = %
v2n = %
'''

def fillInfo(src, data):
        text = src
        for val in data:
            text = text.replace("%", str(val), 1)
        return text


infoN = len(xi) - 1
infoBxn = bound - xi[len(xi)-1]
infoMaxOlp1 = max(olp1i)
infoMaxOlp1X = xi[olp1i.index(infoMaxOlp1)]
infoMaxOlp2 = max(olp2i)
infoMaxOlp2X = xi[olp2i.index(infoMaxOlp2)]
infoC1 = sum(c1i)
infoC2 = sum(c2i)
infoMinHi = min(hi)
infoMinHiXi = xi[hi.index(infoMinHi)]
infoMaxHi = max(hi)
infoMaxHiXi = xi[hi.index(infoMaxHi)]

f = open("info.txt", mode='w', encoding="utf-8")

if (WC):
	f.write(fillInfo(infoTextWC, [infoN, infoBxn, infoMaxOlp1, 
		infoMaxOlp1X, infoC2, infoC1, infoMinHi, infoMinHiXi,
		infoMaxHi, infoMaxHiXi, infoMaxOlp2, infoMaxOlp2X,
		v1i[len(v1i) - 1], v2i[len(v2i) - 1]]))

else:
	f.write(fillInfo(infoText, [infoN, infoBxn, infoMaxOlp1, 
		infoMaxOlp1X, infoMaxOlp2, infoMaxOlp2X,
		v1i[len(v1i) - 1], v2i[len(v2i) - 1] ]))

f.close()