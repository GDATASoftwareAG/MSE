#!/bin/bash

kubectl delete deployment mse
kubectl delete service mse
kubectl delete pvc mse
kubectl delete pv mse