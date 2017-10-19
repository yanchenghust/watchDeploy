<?php
/**
 * @brief 测试
 **/


$return = [
    'errNo'=> 0,
    'errStr'=> 'success',
];
try{
    if(!empty($_FILES)) {
		$ind = 0;
        foreach ($_FILES as $fieldName=> $file) {
            if (!isset($_POST['dest'][$ind])) {
                throw new Exception("dest {$ind} not found", 1);
            }
            $dest = $_POST['dest'][$ind];
            $ret = prepDir($dest);
            if(false === $ret){
                throw new Exception("prep dir $dest fail", 2);
            }
            $ret = move_uploaded_file($file['tmp_name'], $dest);
            if(false === $ret){
                throw new Exception("upload $dest fail", 3);
            }
			$ind ++;
        }
    }

}catch(Exception $e){
    $return = [
        'errNo'=> $e->getCode(),
        'errStr'=> $e->getMessage(),
    ];
}

echo json_encode($return);



function prepDir($path){
    if(file_exists($path)){
        return unlink($path);
    }else{
        $dir = dirname($path);
        if(!file_exists($dir)){
            return mkdir($dir, 0777, true);
        }
    }
    return true;
}
