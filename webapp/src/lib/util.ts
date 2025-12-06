import {addToast} from "@heroui/toast";

export function errorToast(error: {message: string, status?: number}){
    return  addToast({
        color: "danger",
        title: error.status || 'Error!',
        description: error.message || 'Something went wrong'
    });
}

export function handleError(error: {message: string, status?: number}) {
    if (error.status === 500) 
        throw error;
    return errorToast(error);
}