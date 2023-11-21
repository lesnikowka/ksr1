from matplotlib import pyplot as plt
import numpy as np

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

p = 2
max_der = 10**5

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

def saveval(xi_, v1i_, v2i_, hi_):
    global xi, v1i, v2i, v12i, v22i, olp1i, olp2i, c1i, c2i
    xi.append(xi_)
    v1i.append(v1i_)
    v2i.append(v2i_)
    v12i.append(0)
    v22i.append(0)
    olp1i.append(0)
    olp2i.append(0)
    c1i.append(0)
    c2i.append(0)
    hi.append(hi_)

def eraseEnd():
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
    fn = f1(x, v1, v2)
    f1next = f1(x + h, v1 + h * fn, v2 + h * fn)
    f2next = f2(x + h, v1 + h * fn, v2 + h * fn)
    v1next = v1 + (h / 2) * (fn + f1next)
    v2next = v2 + (h / 2) * (fn + f2next)
    x += h
    if not wc:
        saveval(x, v1next, v2next, h)
    return x, v1next, v2next

def stepWC(f1, f2, h, x, v1, v2, e, maxeabs_):
    xnext, v1next, v2next = step(f1, f2, h, x, v1, v2, True)
    x1half, v1half, v2half  = step(f1, f2, h / 2, x, v1, v2, True)
    x12next, v12next, v22next = step(f1, f2, h, x1half, v1half, v2half, True)
    s1 = abs(v1next - v12next)
    s2 = abs(v2next - v22next)
    s = max(s1, s2)
    minbound = e / (2**p - 1)

    divisions = 0

    while h >= 2 / maxeabs_:
        h /= 2
        divisions += 1

    if s > minbound and s <= e:
        savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 0, h)
        return xnext, v1next, v2next, h
    elif s <= minbound:
        if h >= 1 / maxeabs_:
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
            x12next, v12next, v22next = step(f1, f2, h, x1half, v1half, v2half, True)
            s1 = abs(v1next - v12next)
            s2 = abs(v2next - v22next)
            s = max(s1, s2)

        savevalwc(xnext, v1next, v2next, v12next, v22next, s1, s2, divisions, 0, h)

        return xnext, v1next, v2next, h

def RK4(f1, f2, h, x, v1, v2, n, b, eb):
    saveval(x, v1, v2, 0)
    oldx = x
    oldv1 = v1
    oldv2 = v2

    for i in range(1, n + 1):
        x, v1, v2 = step(f1, f2, h, x, v1, v2)
        if f1(x, v1, v2) > max_der or f2(x, v1, v2) > max_der or x >= b - eb and x <= b:
            return
        elif x > b:
            eraseEnd()
            x, v1, v2 = step(f1, f2, b - oldx, oldx, oldv1, oldv2)
            saveval(x, v1, v2, b - oldx)

def RK4WC(f1, f2, h, x, v1, v2, n, b, eb, e, maxeabs_):
    saveval(x, v1, v2, 0)

    for i in range(1, n + 1):
        oldx = x
        oldv1 = v1
        oldv2 = v2

        x, v1, v2, h = stepWC(f1, f2, h, x, v1, v2, e, maxeabs_)
        if f1(x, v1, v2) > max_der or f2(x, v1, v2) > max_der or x >= b - eb and x <= b:
            return
        elif x > b:
            eraseEnd()
            x, v1, v2, h = stepWC(f1, f2, b - oldx, oldx, oldv1, oldv2, e, maxeabs_)
            saveval(x, v1, v2, h)

x0 = 0
u10 = 7
u20 = 13
matr = getMatr()
func1 = createfunc1(matr)
func2 = createfunc2(matr)
maxeabs = max([abs(e) for e in np.linalg.eig(matr)[0]])
Eb = 10**-6
E = 10**-3
bound = 0.05
h_ = 10**-2
N = 100000

#RK4(func1, func2, h_, x0, u10, u20, N, bound, Eb)
RK4WC(func1, func2, h_, x0, u10, u20, N, bound, Eb, E, maxeabs)

#for row in zip(c1i, c2i):
#    print(row[0], row[1])

plt.figure(figsize=(12, 5))

plt.subplot(131)
plt.plot(xi, v1i)
plt.subplot(132)
plt.plot(xi, v2i)
plt.subplot(133)
plt.plot(v1i, v2i)
plt.show()

